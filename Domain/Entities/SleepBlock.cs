using ShiftSync.Domain.Enums;
using System;

namespace ShiftSync.Domain.Entities
{
    public sealed class SleepBlock : TimeBlock
    {
        public SleepBlock(DateTime start, DateTime end)
        {
            Type = BlockType.Sleep;
            Start = start;
            End = end;
        }
    }
}