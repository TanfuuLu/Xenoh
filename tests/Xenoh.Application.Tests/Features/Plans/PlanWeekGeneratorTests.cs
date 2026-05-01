using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Plans;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class PlanWeekGeneratorTests
{
    [Fact]
    public void Generate_WhenPlanStartsThursdayAndEndsWednesday_UsesMondayToSundayWeeks()
    {
        var plan = new Plan
        {
            Name = "Calendar aligned",
            StartDate = new DateOnly(2026, 4, 30),
            EndDate = new DateOnly(2026, 5, 6)
        };

        var weeks = PlanWeekGenerator.Generate(plan);

        weeks.Should().HaveCount(2);
        weeks[0].StartDate.Should().Be(new DateOnly(2026, 4, 27));
        weeks[0].EndDate.Should().Be(new DateOnly(2026, 5, 3));
        weeks[0].DailyWorkouts.Select(d => d.DayOfWeek)
            .Should().Equal(
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday);

        weeks[1].StartDate.Should().Be(new DateOnly(2026, 5, 4));
        weeks[1].EndDate.Should().Be(new DateOnly(2026, 5, 10));
    }
}
