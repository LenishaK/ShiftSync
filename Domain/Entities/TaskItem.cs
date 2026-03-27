using ShiftSync.Domain.Enums;
using systems;

namespace ShiftSync.Domain.Entities
{
    public sealed class TaskItem
    {
        public string Name { get; init; } = string.Empty;
        public TimeSpan Duration { get; init; }
        public Priotity Priority { get; init; }
        public DateTime? Deadline { get; init; }

        public TaskItem()
        {
        }
    }
}