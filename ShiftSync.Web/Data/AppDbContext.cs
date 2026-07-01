using Microsoft.EntityFrameworkCore;
using ShiftSync.Domain.Entities;

namespace ShiftSync.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ShiftData> Shifts { get; set; }
        public DbSet<TaskData> Tasks { get; set; }
        public DbSet<PreferenceData> Preferences { get; set; }
        public DbSet<UserData> Users { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ShiftData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Title { get; set; } = "Work";
        public int CommuteMins { get; set; }
        public bool IsLateShift { get; set; }
    }

    public class TaskData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public double DurationHours { get; set; }
        public DateTime? Deadline { get; set; }
        public int Priority { get; set; }
    }

    public class PreferenceData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MinSleepHours { get; set; } = 8;
        public int WindDownMins { get; set; } = 30;
        public string StudyStart { get; set; } = "10:00";
        public string StudyEnd { get; set; } = "20:00";
        public int MaxStudyHours { get; set; } = 3;
        public int MinBlockMins { get; set; } = 30;
    }
}