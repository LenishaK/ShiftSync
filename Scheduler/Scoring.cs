using ShiftSync.Domain.Entities;
using ShiftSync.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSync.Scheduler
{
    internal static class Scoring
    {
        private const int W_PREFERRED = 3;
        private const int W_FREE_TIME = 2;
        private const int W_PRIORITY_EARLY = 3;
        private const int W_FRAGMENTATION = 2;
        private const int W_OVERTIME_DAY = 4;
        private const int W_FREE_DAY = 3;

        public static int Score(List<TimeBlock> schedule, UserPreference prefs, ScheduleLimits limits)
        {
            int score = 0;

            score += W_PREFERRED * PreferredTimesScore(schedule, prefs);
            score += W_FREE_TIME * FreeTimePreferenceScore(schedule, prefs);
            score += W_PRIORITY_EARLY * PriorityEarlierScore(schedule);
            score -= W_FRAGMENTATION * FragmentationPenalty(schedule);
            score -= W_OVERTIME_DAY * OvertimeDayPenalty(schedule, limits.MaxHoursPerDay);
            score += W_FREE_DAY * FreeDayPreferenceScore(schedule);

            return score;
        }
        private static int FreeDayPreferenceScore(List<TimeBlock> schedule)
        {
            int points = 0;
            var shiftDays = schedule.OfType<Shift>().Select(s => s.Start.Date).ToHashSet();

            foreach (var block in schedule.OfType<TaskBlock>())
            {
                if (!shiftDays.Contains(block.Start.Date))
                    points += 2;
            }

            return points;
        }

        private static int PreferredTimesScore(List<TimeBlock> schedule, UserPreference prefs)
        {
            if (prefs.PreferredWindows.Count == 0)
                return 0;

            int points = 0;

            foreach (var block in schedule.OfType<TaskBlock>())
            {
                var matchesPreferredWindow = prefs.PreferredWindows.Any(window =>
                    window.Day == block.Start.DayOfWeek &&
                    block.Start.TimeOfDay >= window.StartTime &&
                    block.End.TimeOfDay <= window.EndTime);

                if (matchesPreferredWindow)
                    points += 2;
            }

            return points;
        }

        private static int FreeTimePreferenceScore(List<TimeBlock> schedule, UserPreference prefs)
        {
            int points = 0;

            foreach (var block in schedule.OfType<TaskBlock>())
            {
                if (prefs.KeepEveningsFree)
                {
                    if (block.Start.TimeOfDay >= TimeSpan.FromHours(18) ||
                        block.End.TimeOfDay > TimeSpan.FromHours(18))
                    {
                        points -= 2;
                    }
                }

                if (prefs.KeepMorningsFree)
                {
                    if (block.Start.TimeOfDay < TimeSpan.FromHours(10))
                    {
                        points -= 2;
                    }
                }
            }

            return points;
        }

        private static int PriorityEarlierScore(List<TimeBlock> schedule)
        {
            int points = 0;

            var taskBlocks = schedule
                .OfType<TaskBlock>()
                .OrderBy(b => b.Start)
                .ToList();

            for (int i = 0; i < taskBlocks.Count; i++)
            {
                var block = taskBlocks[i];

                if (block.Priority == Priority.High)
                    points += Math.Max(0, 6 - i);

                if (block.Priority == Priority.Medium)
                    points += Math.Max(0, 3 - i);
            }

            return points;
        }

        private static int OvertimeDayPenalty(List<TimeBlock> schedule, double maxHoursPerDay)
        {
            int penalty = 0;

            foreach (var group in schedule.GroupBy(b => b.Start.Date))
            {
                var totalHours = group.Sum(b => (b.End - b.Start).TotalHours);

                if (totalHours > maxHoursPerDay)
                    penalty += 10;
                else if (totalHours > maxHoursPerDay * 0.9)
                    penalty += 3;
            }

            return penalty;
        }

        private static int FragmentationPenalty(List<TimeBlock> schedule)
        {
            int penalty = 0;

            foreach (var day in schedule.GroupBy(b => b.Start.Date))
            {
                var blocks = day.OrderBy(b => b.Start).ToList();

                for (int i = 0; i < blocks.Count - 1; i++)
                {
                    var gap = blocks[i + 1].Start - blocks[i].End;

                    if (gap > TimeSpan.FromHours(2))
                        penalty += 2;
                }
            }

            return penalty;
        }
    }
};