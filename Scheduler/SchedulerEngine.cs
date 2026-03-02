using ShiftSync.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSync
{
    public sealed class SchedulerEngine
    {

        private const int W_PREFERRED = 3;
        private const int W_FREE_MORNING_EVENING= 2;
        private const int W_PRIORITY_EARLY = 3;
        private const int W_FRAGMENTATION = 2;
        private const int W_OVERTIME_DAY = 4;

        public ScheduleResult GenerateSchedule(
            List<SHIFT> shifts,
            List<TaskItem> tasks,
            List<AvailabilityWindow> availability,
            UserPrefrences prefs,
            ScheduleLimits limits)
        {
            var result = new ScheduleResult();

            //start with fixed shifts 
            var schedule = new List<TimeBlock>();
            schedule.AddRange(shifts);
            schedule = schedule.OrderBy(b => b.Start).ToList();

            //sort task: by highest priority, then earliest dealine (if any) 
            var sortedTasks = tasks
                .OrderByDescending(t => (int)t.Priority)
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .ToList();

            foreach (var task in sortedTasks)
            {
                var freeSlots = FreeSlotBuilder.ComputeFreeSlots(availability, schedule);

                var candidateSlots = freeSlots
                   .Where(s => (s.End - s.Start) >= task.Duration)
                   .SelectMany(s => SlotStartCandidates(s, task.Duration, stepMinutes: 30)) // try every 30 mins
                   .ToList();

                TaskBlock? best = null;
                int bestScore = int.MinValue;

                foreach (var start in candidateSlots)
                {
                    var end = start + task.Duration;
                    var block = new TaskBlock(task, start, end);

                    if (ConstraintChecker.ViolatesAny(block, schedule, availability, limits, task))
                        continue;

                    var temp = schedule.Concat(new[] { block }).OrderBy(b => b.Start).ToList();
                    var score = Scoring.Score(temp, prefs, limits);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = block;
                    }
                }

                if (best != null)
                {
                    schedule.Add(best);
                    schedule = schedule.OrderBy(b => b.Start).ToList();
                }
                else
                {
                    result.Warnings.Add($"Could not schedule task: {task.Name}");
                }
            }

            result.Blocks = schedule.OrderBy(b => b.Start).ToList();
            return result;
        }

        private static IEnumerable<DateTime> SlotStartCandidates((DateTime Start, DateTime End) slot, TimeSpan duration, int stepMinutes)
        {
            var t = slot.Start;
            while (t + duration <= slot.End)
            {
                yield return t;
                t = t.AddMinutes(stepMinutes);
            }
        }
    }

    internal static class ConstraintChecker
    {
        public static bool ViolatesAny(
            TaskBlock block,
            List<TimeBlock> schedule,
            List<AvailabilityWindow> availability,
            ScheduleLimits limits,
            TaskItem originalTask)
        {
            if (Overlaps(block, schedule)) return true;
            if (OutsideAvailability(block, availability)) return true;
            if (ViolatesMinRest(block, schedule, limits.MinRestHoursBetweenShifts)) return true;
            if (ExceedsMaxHours(schedule, block, limits.MaxHoursPerDay, limits.MaxHoursPerWeek)) return true;
            if (originalTask.Deadline.HasValue && block.End > originalTask.Deadline.Value) return true;

            return false;
        }

        private static bool Overlaps(TimeBlock block, List<TimeBlock> schedule)
        {
            foreach (var b in schedule)
            {
                // overlap if start < other.end AND end > other.start
                if (block.Start < b.End && block.End > b.Start)
                    return true;
            }
            return false;
        }

        private static bool OutsideAvailability(TimeBlock block, List<AvailabilityWindow> availability)
        {
            // block must fit inside at least one availability window for that day
            var day = block.Start.DayOfWeek;
            var windows = availability.Where(a => a.Day == day).ToList();
            if (windows.Count == 0) return true;

            var startT = block.Start.TimeOfDay;
            var endT = block.End.TimeOfDay;

            return !windows.Any(w => startT >= w.StartTime && endT <= w.EndTime);
        }

        private static bool ViolatesMinRest(TimeBlock block, List<TimeBlock> schedule, double minRestHours)
        {
            // Only enforce rest relative to shifts (as per your rule).
            // Find shifts closest before/after this block
            var shifts = schedule.OfType<Shift>().OrderBy(s => s.Start).ToList();
            if (shifts.Count == 0) return false;

            var minRest = TimeSpan.FromHours(minRestHours);

            // If the block is a task, ensure it doesn't violate rest between shifts by pushing shifts too close.
            // Simple version: enforce that tasks do not sit inside the "rest gap" right after a shift ends
            // or right before a shift starts (acts as protected rest).
            foreach (var s in shifts)
            {
                var restAfterStart = s.End;
                var restAfterEnd = s.End + minRest;

                if (block.Start < restAfterEnd && block.End > restAfterStart)
                {
                    // task overlaps protected rest period after a shift
                    return true;
                }

                var restBeforeStart = s.Start - minRest;
                var restBeforeEnd = s.Start;

                if (block.Start < restBeforeEnd && block.End > restBeforeStart)
                {
                    // task overlaps protected rest period before a shift
                    return true;
                }
            }

            return false;
        }

        private static bool ExceedsMaxHours(List<TimeBlock> schedule, TimeBlock newBlock, double maxDay, double maxWeek)
        {
            var combined = schedule.Concat(new[] { newBlock }).ToList();

            // Day cap
            var dayGroups = combined.GroupBy(b => b.Start.Date);
            foreach (var g in dayGroups)
            {
                var hours = g.Sum(b => (b.End - b.Start).TotalHours);
                if (hours > maxDay) return true;
            }

            // Week cap (simple: 7-day window starting Monday of newBlock week)
            var weekStart = StartOfWeek(newBlock.Start.Date, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(7);

            var weekBlocks = combined.Where(b => b.Start >= weekStart && b.Start < weekEnd).ToList();
            var weekHours = weekBlocks.Sum(b => (b.End - b.Start).TotalHours);
            if (weekHours > maxWeek) return true;

            return false;
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startDay)
        {
            int diff = (7 + (date.DayOfWeek - startDay)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }

    internal static class FreeSlotBuilder
    {
        public static List<(DateTime Start, DateTime End)> ComputeFreeSlots(List<AvailabilityWindow> availability, List<TimeBlock> schedule)
        {
            // Build daily windows for the current week based on schedule dates.
            // For demo/testing: derive the dates from the schedule range (or default to this week).
            var dates = GetRelevantDates(schedule);
            var slots = new List<(DateTime Start, DateTime End)>();

            foreach (var date in dates)
            {
                var windows = availability.Where(a => a.Day == date.DayOfWeek).ToList();
                foreach (var w in windows)
                {
                    var winStart = date.Date + w.StartTime;
                    var winEnd = date.Date + w.EndTime;

                    // subtract existing blocks
                    var dayBlocks = schedule.Where(b => b.Start.Date == date.Date).OrderBy(b => b.Start).ToList();
                    var free = SubtractBlocksFromWindow(winStart, winEnd, dayBlocks);
                    slots.AddRange(free);
                }
            }

            return slots.OrderBy(s => s.Start).ToList();
        }

        private static List<DateTime> GetRelevantDates(List<TimeBlock> schedule)
        {
            if (schedule.Count == 0)
            {
                // default: next 7 days from today
                return Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(i)).ToList();
            }

            var min = schedule.Min(b => b.Start.Date);
            var max = schedule.Max(b => b.Start.Date);
            var days = (max - min).Days;

            // cover at least 7 days
            var span = Math.Max(days, 6);
            return Enumerable.Range(0, span + 1).Select(i => min.AddDays(i)).ToList();
        }

        private static List<(DateTime Start, DateTime End)> SubtractBlocksFromWindow(DateTime windowStart, DateTime windowEnd, List<TimeBlock> blocks)
        {
            var free = new List<(DateTime Start, DateTime End)>();
            var cursor = windowStart;

            foreach (var b in blocks)
            {
                if (b.End <= cursor) continue;
                if (b.Start >= windowEnd) break;

                var blockStart = b.Start < windowStart ? windowStart : b.Start;
                var blockEnd = b.End > windowEnd ? windowEnd : b.End;

                if (blockStart > cursor)
                    free.Add((cursor, blockStart));

                cursor = blockEnd;
            }

            if (cursor < windowEnd)
                free.Add((cursor, windowEnd));

            // Remove tiny slots
            return free.Where(s => (s.End - s.Start).TotalMinutes >= 30).ToList();
        }
    }

    internal static class Scoring
    {
        public static int Score(List<TimeBlock> schedule, UserPreferences prefs, ScheduleLimits limits)
        {
            int score = 0;

            score += 3 * PreferredTimesScore(schedule, prefs);
            score += 2 * FreeTimePreferenceScore(schedule, prefs);
            score += 3 * PriorityEarlierScore(schedule);
            score -= 2 * FragmentationPenalty(schedule);
            score -= 4 * OvertimeDayPenalty(schedule, limits.MaxHoursPerDay);

            return score;
        }

        private static int PreferredTimesScore(List<TimeBlock> schedule, UserPreferences prefs)
        {
            if (prefs.PreferredWindows.Count == 0) return 0;

            int points = 0;
            foreach (var b in schedule.OfType<TaskBlock>())
            {
                var matches = prefs.PreferredWindows.Any(w =>
                    w.Day == b.Start.DayOfWeek &&
                    b.Start.TimeOfDay >= w.StartTime &&
                    b.End.TimeOfDay <= w.EndTime);

                if (matches) points += 2;
            }
            return points;
        }

        private static int FreeTimePreferenceScore(List<TimeBlock> schedule, UserPreferences prefs)
        {
            int points = 0;

            foreach (var b in schedule.OfType<TaskBlock>())
            {
                if (prefs.KeepEveningsFree)
                {
                    // penalize tasks that touch evening (18:00+)
                    if (b.Start.TimeOfDay >= TimeSpan.FromHours(18) || b.End.TimeOfDay > TimeSpan.FromHours(18))
                        points -= 2;
                }

                if (prefs.KeepMorningsFree)
                {
                    // penalize tasks that start before 10:00
                    if (b.Start.TimeOfDay < TimeSpan.FromHours(10))
                        points -= 2;
                }
            }

            return points;
        }

        private static int PriorityEarlierScore(List<TimeBlock> schedule)
        {
            // Reward if high priority tasks are earlier in the week/day
            // Simple: give points for high priority tasks scheduled earlier date
            int points = 0;

            var taskBlocks = schedule.OfType<TaskBlock>().OrderBy(b => b.Start).ToList();
            for (int i = 0; i < taskBlocks.Count; i++)
            {
                var tb = taskBlocks[i];
                if (tb.Priority == Priority.High) points += Math.Max(0, 6 - i);   // earlier = more points
                if (tb.Priority == Priority.Medium) points += Math.Max(0, 3 - i);
            }

            return points;
        }

        private static int OvertimeDayPenalty(List<TimeBlock> schedule, double maxHoursPerDay)
        {
            int penalty = 0;
            foreach (var g in schedule.GroupBy(b => b.Start.Date))
            {
                var hours = g.Sum(b => (b.End - b.Start).TotalHours);
                if (hours > maxHoursPerDay) penalty += 10; // strong penalty (even though constraint should block it)
                else if (hours > maxHoursPerDay * 0.9) penalty += 3; // discourage near-max days
            }
            return penalty;
        }

        private static int FragmentationPenalty(List<TimeBlock> schedule)
        {
            // “lessen days with blocks far apart”
            // Penalize big gaps between blocks on the same day (e.g., > 2 hours)
            int penalty = 0;

            foreach (var day in schedule.GroupBy(b => b.Start.Date))
            {
                var blocks = day.OrderBy(b => b.Start).ToList();
                for (int i = 0; i < blocks.Count - 1; i++)
                {
                    var gap = blocks[i + 1].Start - blocks[i].End;
                    if (gap > TimeSpan.FromHours(2)) penalty += 2;
                }
            }

            return penalty;
        }
    }
}
            }
        }
}
