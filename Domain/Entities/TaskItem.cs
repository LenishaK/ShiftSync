using ShiftSync.Domain.Enums;
using System;

namespace ShiftSync.Domain.Entities
{
    public sealed class TaskItem
    {
        public string Name { get; init; } = string.Empty;
        public TimeSpan Duration { get; init; }
        public Priority Priority { get; init; }
        public DateTime? Deadline { get; init; }

        public TaskItem()
        {
        }
    }
}