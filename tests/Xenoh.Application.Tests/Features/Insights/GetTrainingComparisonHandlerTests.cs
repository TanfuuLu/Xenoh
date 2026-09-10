using FluentAssertions;
using Xenoh.Application.Features.Insights.Queries.GetTrainingComparison;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class GetTrainingComparisonHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_ReturnsOnlyTheCurrentUsersSelectedLiftAcrossEqualPeriods()
    {
        await using var db = CreateContext();
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var bench = new ExerciseTemplate { Name = "Bench Press", PrimaryMuscleGroup = MuscleGroup.Chest };
        var squat = new ExerciseTemplate { Name = "Squat", PrimaryMuscleGroup = MuscleGroup.Quads };
        db.ExerciseTemplates.AddRange(bench, squat);

        AddSet(db, UserId, bench, asOf.AddDays(-1), 5, 5, 100m, 8m);
        AddSet(db, UserId, bench, asOf.AddDays(-8), 5, 5, 95m, 7m);
        AddSet(db, UserId, squat, asOf.AddDays(-1), 5, 5, 120m, 8m);
        AddSet(db, Guid.NewGuid(), bench, asOf.AddDays(-1), 5, 5, 200m, 10m);
        await db.SaveChangesAsync();

        var result = await new GetTrainingComparisonHandler(db, CurrentUser())
            .Handle(new GetTrainingComparisonQuery(7, bench.Id), CancellationToken.None);

        result.Scope.Days.Should().Be(7);
        result.Scope.ExerciseTemplateId.Should().Be(bench.Id);
        result.Scope.ExerciseName.Should().Be("Bench Press");
        result.Current.CompletedSetCount.Should().Be(1);
        result.Previous.CompletedSetCount.Should().Be(1);
        result.Current.BestEstimatedOneRepMax.Should().Be(116.67m);
        result.AvailableLifts.Should().Contain(x => x.Id == bench.Id && x.Name == "Bench Press");
        result.AvailableLifts.Should().Contain(x => x.Id == squat.Id && x.Name == "Squat");
        result.Sessions.Should().OnlyContain(x => x.ExerciseTemplateId == bench.Id);
    }

    [Fact]
    public async Task Handle_RejectsARequestedLiftWithoutTheUsersTrainingHistory()
    {
        await using var db = CreateContext();
        var template = new ExerciseTemplate { Name = "Bench Press", PrimaryMuscleGroup = MuscleGroup.Chest };
        db.ExerciseTemplates.Add(template);
        await db.SaveChangesAsync();

        var action = () => new GetTrainingComparisonHandler(db, CurrentUser())
            .Handle(new GetTrainingComparisonQuery(7, template.Id), CancellationToken.None)
            .AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The selected lift has no training history.");
    }

    private static void AddSet(
        Xenoh.Infrastructure.Persistence.ApplicationDbContext db,
        Guid ownerId,
        ExerciseTemplate template,
        DateOnly date,
        int plannedReps,
        int actualReps,
        decimal actualWeight,
        decimal rpe)
    {
        var plan = new Plan { OwnerId = ownerId, Name = "Plan", PlanType = PlanType.Self, StartDate = date, EndDate = date };
        var week = new WeeklyWorkout { Plan = plan, PlanId = plan.Id, WeekNumber = 1, Name = "Week", StartDate = date, EndDate = date };
        var day = new DailyWorkout { WeeklyWorkout = week, WeeklyWorkoutId = week.Id, Date = date, DayOfWeek = date.DayOfWeek, IsCompleted = true };
        var exercise = new Exercise
        {
            DailyWorkout = day,
            DailyWorkoutId = day.Id,
            ExerciseTemplate = template,
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = template.PrimaryMuscleGroup,
            PlannedSets = 1,
            PlannedReps = plannedReps,
            IsCompleted = true,
        };
        exercise.Sets.Add(new ExerciseSet
        {
            Exercise = exercise,
            ExerciseId = exercise.Id,
            SetNumber = 1,
            PlannedReps = plannedReps,
            ActualReps = actualReps,
            ActualWeight = actualWeight,
            Rpe = rpe,
            IsCompleted = true,
        });
        day.Exercises.Add(exercise);
        week.DailyWorkouts.Add(day);
        plan.WeeklyWorkouts.Add(week);
        db.Plans.Add(plan);
    }
}
