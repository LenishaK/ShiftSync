using ShiftSync.Domain.Entities;
using ShiftSync.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSync.Scheduler
{
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
            if (ExceedsMaxHours(schedule, block, limits.MaxHoursPerDay, limits.MaxHoursPerWeek)) return true;
            if (originalTask.Deadline.HasValue && block.End > originalTask.Deadline.Value) return true;
            return false;
        }

        private static bool Overlaps(TimeBlock block, List<TimeBlock> schedule)
        {
            foreach (var existing in schedule)
            {
                if (block.Start < existing.End && block.End > existing.Start)
                    return true;
            }
            return false;
        }

        private static bool OutsideAvailability(TimeBlock block, List<AvailabilityWindow> availability)
        {
            var day = block.Start.DayOfWeek;
            var windows = availability.Where(a => a.Day == day).ToList();
            if (windows.Count == 0)
                return true;
            var startTime = block.Start.TimeOfDay;
            var endTime = block.End.TimeOfDay;
            return !windows.Any(w =>
                startTime >= w.StartTime &&
                endTime <= w.EndTime);
        }

        private static bool ExceedsMaxHours(
            List<TimeBlock> schedule,
            TimeBlock newBlock,
            double maxHoursPerDay,
            double maxHoursPerWeek)
        {
            var combined = schedule.Concat(new[] { newBlock }).ToList();

            // Daily cap - only count task blocks not shifts or sleep
            foreach (var dayGroup in combined.GroupBy(b => b.Start.Date))
            {
                var totalHours = dayGroup
                    .Where(b => b.Type == BlockType.Task)
                    .Sum(b => (b.End - b.Start).TotalHours);
                if (totalHours > maxHoursPerDay)
                    return true;
            }

            // Weekly cap
            var weekStart = StartOfWeek(newBlock.Start.Date, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(7);
            var weekBlocks = combined
                .Where(b => b.Start >= weekStart && b.Start < weekEnd && b.Type == BlockType.Task)
                .ToList();
            var weeklyHours = weekBlocks.Sum(b => (b.End - b.Start).TotalHours);
            return weeklyHours > maxHoursPerWeek;
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startDay)
        {
            int diff = (7 + (date.DayOfWeek - startDay)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}