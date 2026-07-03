using ShiftSync.Domain.Entities;
using ShiftSync.Web.Data;

namespace ShiftSync.Web.Services
{
    public class AppState
    {
        public UserData? CurrentUser { get; set; }
        public bool IsLoggedIn => CurrentUser != null;

        public List<Shift> Shifts { get; set; } = new();
        public List<TaskItem> Tasks { get; set; } = new();
        public List<TimeBlock> GeneratedSchedule { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int MinSleepHours { get; set; } = 8;
        public int WindDownMins { get; set; } = 30;
        public int MaxStudyHours { get; set; } = 3;
        public int MinBlockMins { get; set; } = 30;
        public UserPreference? Preferences { get; set; }

        public event Action? OnChange;
        public void NotifyStateChanged() => OnChange?.Invoke();

        public void Logout()
        {
            CurrentUser = null;
            Shifts = new();
            Tasks = new();
            GeneratedSchedule = new();
            Warnings = new();
        }
    }
}