using ShiftSync.Domain.Entities;
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
            schedule = schedule.OrderBy(b => b.Start).ToList();

            // Sort tasks by priority first, then earliest deadline
            var sortedTasks = tasks
                .OrderByDescending(t => (int)t.Priority)
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .ToList();

            foreach (var task in sortedTasks)
            {
                var freeSlots = FreeSlotBuilder.ComputeFreeSlots(availability, schedule);

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

                    if (ConstraintChecker.ViolatesAny(block, schedule, availability, limits, task))
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
    }
}