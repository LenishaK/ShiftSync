using ShiftSync.Domain.Entities;
using ShiftSync.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSync.Scheduler
{
    public sealed class SchedulerEngine
    {
        public ScheduleResult GenerateSchedule(
            List<ShiftSync.Domain.Entities.Shift> shifts,
            List<TaskItem> tasks,
            List<AvailabilityWindow> availability,
            UserPreference prefs,
            ScheduleLimits limits)
        {
            if (shifts == null) throw new ArgumentNullException(nameof(shifts));
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));
            if (availability == null) throw new ArgumentNullException(nameof(availability));
            if (prefs == null) throw new ArgumentNullException(nameof(prefs));
            if (limits == null) throw new ArgumentNullException(nameof(limits));

            var result = new ScheduleResult();

            // Start with fixed shifts
            var schedule = new List<TimeBlock>();
            schedule.AddRange(shifts);

            // Get all relevant dates - from first shift to last deadline
            var firstDate = shifts.Any() ? shifts.Min(s => s.Start.Date) : DateTime.Today;
            var lastDate = tasks.Any() && tasks.Any(t => t.Deadline.HasValue)
                ? tasks.Where(t => t.Deadline.HasValue).Max(t => t.Deadline!.Value.Date)
                : firstDate.AddDays(7);

            var dates = Enumerable.Range(0, (lastDate - firstDate).Days + 1)
                .Select(i => firstDate.AddDays(i))
                .ToList();

            // Build availability for all dates
            var fullAvailability = dates.Select(d => new AvailabilityWindow
            {
                Day = d.DayOfWeek,
                StartTime = TimeSpan.FromHours(6),
                EndTime = TimeSpan.FromHours(23)
            }).ToList();

            // Add sleep blocks
            var sleepBlocks = GenerateSleepBlocks(shifts, dates,
                limits.MinSleepHours, limits.WindDownMins);
            schedule.AddRange(sleepBlocks);

            schedule = schedule.OrderBy(b => b.Start).ToList();

            // Sort tasks by priority first, then earliest deadline
            var sortedTasks = tasks
                .OrderByDescending(t => (int)t.Priority)
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .ToList();

            foreach (var task in sortedTasks)
            {
                var freeSlots = FreeSlotBuilder.ComputeFreeSlots(fullAvailability, schedule);

                var candidateStarts = freeSlots
                    .Where(slot => (slot.End - slot.Start) >= task.Duration)
                    .SelectMany(slot => SlotStartCandidates(slot, task.Duration, 30))
                    .ToList();

                TaskBlock? bestBlock = null;
                int bestScore = int.MinValue;

                foreach (var start in candidateStarts)
                {
                    var end = start + task.Duration;
                    var block = new TaskBlock(task, start, end);

                    if (ConstraintChecker.ViolatesAny(block, schedule, fullAvailability, limits, task))
                        continue;

                    var tempSchedule = schedule
                        .Concat(new[] { block })
                        .OrderBy(b => b.Start)
                        .ToList();

                    var score = Scoring.Score(tempSchedule, prefs, limits);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestBlock = block;
                    }
                }

                if (bestBlock != null)
                {
                    schedule.Add(bestBlock);
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

        private static IEnumerable<DateTime> SlotStartCandidates(
            (DateTime Start, DateTime End) slot,
            TimeSpan duration,
            int stepMinutes)
        {
            var current = slot.Start;

            while (current + duration <= slot.End)
            {
                yield return current;
                current = current.AddMinutes(stepMinutes);
            }
        }

        private static List<TimeBlock> GenerateSleepBlocks(
            List<ShiftSync.Domain.Entities.Shift> shifts,
            List<DateTime> dates,
            int minSleepHours,
            int windDownMins)
        {
            var sleepBlocks = new List<TimeBlock>();

            foreach (var date in dates)
            {
                var dayShift = shifts.FirstOrDefault(s => s.Start.Date == date.Date);

                DateTime sleepStart;

                var earliestBedtime = date.Date.AddHours(23); // never sleep before 10pm

                if (dayShift != null)
                {
                    var afterWindDown = dayShift.End.AddMinutes(windDownMins);
                    sleepStart = afterWindDown > earliestBedtime ? afterWindDown : earliestBedtime;
                }
                else
                {
                    sleepStart = earliestBedtime;
                }

                var sleepEnd = sleepStart.AddHours(minSleepHours);
                sleepBlocks.Add(new SleepBlock(sleepStart, sleepEnd));
            }

            return sleepBlocks;
        }
    }
}