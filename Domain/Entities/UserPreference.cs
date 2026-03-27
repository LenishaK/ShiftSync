using System.Collections.Generic;

namespace ShiftSync.Domain.Entities
{
    public sealed class UserPreference
    {
        public bool KeepMoriningsFree { get; init; }
        public bool KeepEveningsFree { get; init; }
        public List<PreferredWindow> PreferredWindows { get; init; } = new();
    }
}