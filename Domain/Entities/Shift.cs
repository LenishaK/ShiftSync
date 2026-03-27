using ShiftSync.Domain.Enums;
using systems;

namespace ShiftSync.Domain.Entities
{
    public sealed class Shift : TimeBlock
    {
        public string Title { get; init; } = "Shift";

        public Shift(DateTime start, DateTime end, string? title = null)
        {
            if (end <= start)
                throw new ArgumentException("Shift end time must be after start time.");

            Type = BlockType.Shift;
            start = start;
            End = end;

            if (!string.IsNullOrWhiteSpace(title))
                Title = title;
        }
    }
}