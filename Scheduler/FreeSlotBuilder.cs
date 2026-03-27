using ShiftSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSync.Scheduler
{
    internal static class FreeSlotBuilder
    {
        public static List<(DateTime Start, DateTime End)> ComputeFreeSlots(
            List<AvailabilityWindow> availability,
            List<TimeBlock> schedule)
        {
            var dates = GetRelevantDates(schedule);
            var slots = new List<(DateTime Start, DateTime End)>();

            foreach (var date in dates)
            {
                var windows = availability
                    .Where(a => a.Day == date.DayOfWeek)
                    .ToList();

                foreach (var window in windows)
                {
                    var windowStart = date.Date + window.StartTime;
                    var windowEnd = date.Date + window.EndTime;

                    var dayBlocks = schedule
                        .Where(b => b.Start.Date == date.Date)
                        .OrderBy(b => b.Start)
                        .ToList();

                    var freeSlots = SubtractBlocksFromWindow(windowStart, windowEnd, dayBlocks);
                    slots.AddRange(freeSlots);
                }
            }

            return slots.OrderBy(s => s.Start).ToList();
        }

        private static List<DateTime> GetRelevantDates(List<TimeBlock> schedule)
        {
            if (schedule.Count == 0)
            {
                return Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(i))
                    .ToList();
            }

            var minDate = schedule.Min(b => b.Start.Date);
            var maxDate = schedule.Max(b => b.Start.Date);
            var daySpan = (maxDate - minDate).Days;

            var span = Math.Max(daySpan, 6);

            return Enumerable.Range(0, span + 1)
                .Select(i => minDate.AddDays(i))
                .ToList();
        }

        private static List<(DateTime Start, DateTime End)> SubtractBlocksFromWindow(
            DateTime windowStart,
            DateTime windowEnd,
            List<TimeBlock> blocks)
        {
            var free = new List<(DateTime Start, DateTime End)>();
            var cursor = windowStart;

            foreach (var block in blocks)
            {
                if (block.End <= cursor)
                    continue;

                if (block.Start >= windowEnd)
                    break;

                var blockStart = block.Start < windowStart ? windowStart : block.Start;
                var blockEnd = block.End > windowEnd ? windowEnd : block.End;

                if (blockStart > cursor)
                    free.Add((cursor, blockStart));

                cursor = blockEnd;
            }

            if (cursor < windowEnd)
                free.Add((cursor, windowEnd));

            return free
                .Where(slot => (slot.End - slot.Start).TotalMinutes >= 30)
                .ToList();
        }
    }
}