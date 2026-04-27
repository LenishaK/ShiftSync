using System.Collections.Generic;

namespace ShiftSync.Domain.Entities
{
    public sealed class UserPreference
    {
        public bool KeepMorningsFree { get; init; }
        public bool KeepEveningsFree { get; init; }
        public List<AvailabilityWindow> PreferredWindows { get; init; } = new();
    }
}