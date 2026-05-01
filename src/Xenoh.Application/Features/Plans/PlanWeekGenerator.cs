using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Plans;

internal static class PlanWeekGenerator
{
    internal static List<WeeklyWorkout> Generate(Plan plan)
    {
        var weeks = new List<WeeklyWorkout>();
        var current = StartOfWeek(plan.StartDate);
        var final = EndOfWeek(plan.EndDate);
        int weekNumber = 1;

        while (current <= final)
        {
            var weekEnd = current.AddDays(6);

            var week = new WeeklyWorkout
            {
                WeekNumber = weekNumber,
                Name = $"Tuần {weekNumber} ({current:dd/MM} - {weekEnd:dd/MM})",
                StartDate = current,
                EndDate = weekEnd,
                PlanId = plan.Id,
                DailyWorkouts = GenerateDays(current, weekEnd)
            };

            weeks.Add(week);
            current = current.AddDays(7);
            weekNumber++;
        }

        return weeks;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private static DateOnly EndOfWeek(DateOnly date) => StartOfWeek(date).AddDays(6);

    private static List<DailyWorkout> GenerateDays(DateOnly weekStart, DateOnly weekEnd)
    {
        var days = new List<DailyWorkout>();
        var current = weekStart;

        while (current <= weekEnd)
        {
            days.Add(new DailyWorkout
            {
                Date = current,
                DayOfWeek = current.DayOfWeek,
                IsCompleted = false
            });
            current = current.AddDays(1);
        }

        return days;
    }
}
