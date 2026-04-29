using ShiftSync.Domain.Enums;
using System;

namespace ShiftSync.Domain.Entities
{
    public sealed class Shift : TimeBlock
    {
        public string Title { get; init; } = "Shift";
        public int CommuteMins { get; init; } = 0;
        public bool IsLateShift { get; init; } = false;

        public Shift(DateTime start, DateTime end, string? title = null, int commuteMins = 0, bool isLateShift = false)
        {
            if (end <= start)
                throw new ArgumentException("Shift end time must be after start time.");

            Type = BlockType.Shift;
            CommuteMins = commuteMins;
            IsLateShift = isLateShift;
            Start = start;
            End = end;

            if (!string.IsNullOrWhiteSpace(title))
                Title = title;
        }
    }
}