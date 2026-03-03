using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSync.Domain;

public abstract class TimeBlock
{
    public Guid Id { get; } = Guid.NewGuid();
    public BlockType Type { get; protected set; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}

public sealed class Shift: TimeBlock
{
    public string Title { get; init; } = "Shift";

    public Shift(DateTime start, DateTime end, string? title = null)
    {
        Type = BlockType.Shift;
        Start = start;
        End = end;
        if (!string.IsNullOrWhiteSpace(title)) Title = title!;
    }
}

public sealed class  TaskItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "";
    public TimeSpan Duration { get; init; }
    public Priority Priority { get; init; } = Priority.Medium;
    public DateTime? Deadline { get; init; }  // optional deadline for tasks 
}

public sealed class TaskBlock : TimeBlock
{
    public Guid TaskId { get; }
    public string Name { get; }
    public Priority Priority { get; }
    
    public TaskBlock(TaskItem task, DateTime start, DateTime end)
    {
        Type = BlockType.Task;
        TaskId = task.Id;
        Name = task.Name;
        Priority = task.Priority;
        Start = start;
        End = end;
    }

}

public sealed class AvailabilityWindow
{
    public DayOfWeek Day {  get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}

public sealed class UserPreference
{
    public bool KeepEveningsFree { get; init; }
    public bool KeepMorningsFree { get; init; }

    //optional prefrence 
    public System.Collections.Generic.List<AvailabilityWindow> PreferredWindows { get; init; } = new();
}

public sealed class ScheduleLimits
{
    public double MaxHoursPerDay { get; init; } = 8;
    public double MaxHoursPerWeek { get; init; } = 40;
    public double MinRestHoursBetweenShifts { get; init; } = 11;
}

public sealed class ScheduleResult
{
    public System.Collections.Generic.List<TimeBlock> Blocks { get; set; } = new();
    public System.Collections.Generic.List<string> Warnings { get; set; } = new();

}
