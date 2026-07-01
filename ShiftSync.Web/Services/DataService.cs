using Microsoft.EntityFrameworkCore;
using ShiftSync.Domain.Entities;
using ShiftSync.Domain.Enums;
using ShiftSync.Web.Data;

namespace ShiftSync.Web.Services
{
    public class DataService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DataService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Shift>> LoadShiftsAsync()
        {
            using var db = _factory.CreateDbContext();
            var data = await db.Shifts.ToListAsync();
            return data.Select(s => new Shift(s.Start, s.End, s.Title, s.CommuteMins, s.IsLateShift)).ToList();
        }

        public async Task SaveShiftAsync(Shift shift)
        {
            using var db = _factory.CreateDbContext();
            db.Shifts.Add(new ShiftData
            {
                Start = shift.Start,
                End = shift.End,
                Title = shift.Title,
                CommuteMins = shift.CommuteMins,
                IsLateShift = shift.IsLateShift
            });
            await db.SaveChangesAsync();
        }

        public async Task DeleteShiftAsync(Shift shift)
        {
            using var db = _factory.CreateDbContext();
            var item = await db.Shifts.FirstOrDefaultAsync(s =>
                s.Start == shift.Start && s.End == shift.End);
            if (item != null)
            {
                db.Shifts.Remove(item);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<TaskItem>> LoadTasksAsync()
        {
            using var db = _factory.CreateDbContext();
            var data = await db.Tasks.ToListAsync();
            return data.Select(t => new TaskItem
            {
                Name = t.Name,
                Duration = TimeSpan.FromHours(t.DurationHours),
                Deadline = t.Deadline,
                Priority = (Priority)t.Priority
            }).ToList();
        }

        public async Task SaveTaskAsync(TaskItem task)
        {
            using var db = _factory.CreateDbContext();
            db.Tasks.Add(new TaskData
            {
                Name = task.Name,
                DurationHours = task.Duration.TotalHours,
                Deadline = task.Deadline,
                Priority = (int)task.Priority
            });
            await db.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(TaskItem task)
        {
            using var db = _factory.CreateDbContext();
            var item = await db.Tasks.FirstOrDefaultAsync(t => t.Name == task.Name);
            if (item != null)
            {
                db.Tasks.Remove(item);
                await db.SaveChangesAsync();
            }
        }

        public async Task<PreferenceData?> LoadPreferencesAsync()
        {
            using var db = _factory.CreateDbContext();
            return await db.Preferences.FirstOrDefaultAsync();
        }

        public async Task SavePreferencesAsync(PreferenceData prefs)
        {
            using var db = _factory.CreateDbContext();
            var existing = await db.Preferences.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.MinSleepHours = prefs.MinSleepHours;
                existing.WindDownMins = prefs.WindDownMins;
                existing.StudyStart = prefs.StudyStart;
                existing.StudyEnd = prefs.StudyEnd;
                existing.MaxStudyHours = prefs.MaxStudyHours;
                existing.MinBlockMins = prefs.MinBlockMins;
            }
            else
            {
                db.Preferences.Add(prefs);
            }
            await db.SaveChangesAsync();
        }
    }
}