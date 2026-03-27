namespace ShiftSync.Domain.Entities
{
    public sealed class ScheduleLimits
    {
        public double MaxHoursPerDay { get; init; }
        public double MaxHoursPerWeek { get; init; }
        public double MinRestHoursBetweenShifts { get; init; }
    }
}