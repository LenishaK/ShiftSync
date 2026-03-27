using System.Collections.Generic;

namespace ShiftSync.Domain.Entities
{
    public sealed class ScheduleResult
    {
        public List<TimeBlock> Blocks { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}