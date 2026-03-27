using System;

namespace ShiftSync.Domain.Entities
{
    public sealed class PreferredWindow
    {
        public DayOfWeek Day { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }

        public bool IsValid()
        {
            return EndTime > StartTime;
        }
    }
}