using ShiftSync;
using ShiftSync.Domain;

// ---- DEMO DATA ----

// Create some fixed shifts
var shifts = new List<Shift>
{
    new Shift(DateTime.Today.AddDays(1).AddHours(9), DateTime.Today.AddDays(1).AddHours(17), "Work"),
    new Shift(DateTime.Today.AddDays(3).AddHours(12), DateTime.Today.AddDays(3).AddHours(20), "Work")
};

// Create some tasks
var tasks = new List<TaskItem>
{
    new TaskItem { Name = "Assignment 1", Duration = TimeSpan.FromHours(2), Priority = Priority.High, Deadline = DateTime.Today.AddDays(2) },
    new TaskItem { Name = "Gym", Duration = TimeSpan.FromHours(1), Priority = Priority.Medium },
    new TaskItem { Name = "Read", Duration = TimeSpan.FromHours(1.5), Priority = Priority.Low },
    new TaskItem { Name = "Project Work", Duration = TimeSpan.FromHours(3), Priority = Priority.High }
};

// Availability (Mon–Sun 8am–10pm)
var availability = Enum.GetValues<DayOfWeek>()
    .Select(d => new AvailabilityWindow
    {
        Day = d,
        StartTime = TimeSpan.FromHours(8),
        EndTime = TimeSpan.FromHours(22)
    })
    .ToList();

// Preferences
var prefs = new UserPreference
{
    KeepEveningsFree = true,
    KeepMorningsFree = false,
    PreferredWindows = new List<AvailabilityWindow>
    {
        new AvailabilityWindow
        {
            Day = DayOfWeek.Tuesday,
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(14)
        }
    }
};

// Limits
var limits = new ScheduleLimits
{
    MaxHoursPerDay = 8,
    MaxHoursPerWeek = 40,
    MinRestHoursBetweenShifts = 11
};

// ---- RUN ENGINE ----

var engine = new SchedulerEngine();
var result = engine.GenerateSchedule(shifts, tasks, availability, prefs, limits);

// ---- OUTPUT ----

Console.WriteLine("====== GENERATED SCHEDULE ======\n");

foreach (var block in result.Blocks.OrderBy(b => b.Start))
{
    var label = block.Type == BlockType.Shift
        ? ((Shift)block).Title
        : ((TaskBlock)block).Name;

    Console.WriteLine($"{block.Start:g} - {block.End:g} | {block.Type} | {label}");
}

if (result.Warnings.Any())
{
    Console.WriteLine("\n====== WARNINGS ======");
    foreach (var w in result.Warnings)
        Console.WriteLine(w);
}
else
{
    Console.WriteLine("\nNo scheduling warnings 🎉");
}