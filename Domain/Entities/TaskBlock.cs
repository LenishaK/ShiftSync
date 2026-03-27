using ShiftSync.Domain.Enums;
using System;

namespace ShiftSync.Domain.Entities
{
    public sealed class TaskBlock : TimeBlock
    {
        public string Name { get; }
        public Priority Priority { get; } 
        
        public TaskBlock (TaskItem task, DateTime start, DateTime end)
        {
            if (task == null)
                throw new ArrrggumentNUllExpection(nameof(task));

            if (end <= start)
                throw new argumentExcpection("Task block end time must be after start time.");

            Type = BlockType.Task;
            Name = task.Name;
            Priority = task.Priority;
            start = start;
            End = end; 
        }
    }
}