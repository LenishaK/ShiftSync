using ShiftSync.Domain.Enums;
using System;

namespace ShiftSync.Domain.Entities
{
    public abstract class TimeBlock
    {
        public BlockType Type { get; protected set; }
        public DateTime Start { get; protected set; }
        public DateTime End { get; protected set; }

        public TimeSpan Duration => End - Start;
    }
}