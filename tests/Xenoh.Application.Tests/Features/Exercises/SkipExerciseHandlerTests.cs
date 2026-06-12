using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises.Commands.SkipExercise;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class SkipExerciseHandlerTests : HandlerTestBase
{
    private SkipExerciseHandler CreateHandler(ApplicationDbContext ctx) =>
        new(
            new ExerciseRepository(ctx),
            new BodyweightRepository(ctx),
            new UserPrRepository(ctx),
            ctx,
            CurrentUser());

    [Fact]
    public async Task Handle_WhenExerciseSkipped_CompletesDayAndWeekWithoutCompletingSets()
    {
        var exerciseId = await SeedDayAsync(UserId, completedExerciseCount: 1, incompleteExerciseCount: 1);

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(
            new SkipExerciseCommand { ExerciseId = exerciseId, IsSkipped = true },
            CancellationToken.None);

        response.IsSkipped.Should().BeTrue();
        response.IsCompleted.Should().BeFalse();
        response.Sets.Should().OnlyContain(s => !s.IsCompleted);

        await using var verify = CreateContext();
        verify.DailyWorkouts.Single().IsCompleted.Should().BeTrue();
        verify.WeeklyWorkouts.Single().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenExerciseUnskipped_MakesDayAndWeekIncomplete()
    {
        var exerciseId = await SeedDayAsync(UserId, completedExerciseCount: 1, skippedExerciseCount: 1);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(
            new SkipExerciseCommand { ExerciseId = exerciseId, IsSkipped = false },
            CancellationToken.None);

        await using var verify = CreateContext();
        verify.Exercises.Single(e => e.Id == exerciseId).IsSkipped.Should().BeFalse();
        verify.DailyWorkouts.Single().IsCompleted.Should().BeFalse();
        verify.WeeklyWorkouts.Single().IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSetAlreadyCompleted_Throws()
    {
        var exerciseId = await SeedDayAsync(UserId, partialExerciseCount: 1);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(
            new SkipExerciseCommand { ExerciseId = exerciseId, IsSkipped = true },
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot skip an exercise after sets have been completed.");
    }

    [Fact]
    public async Task Handle_WhenNonOwner_Throws()
    {
        var exerciseId = await SeedDayAsync(Guid.NewGuid(), incompleteExerciseCount: 1);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(
            new SkipExerciseCommand { ExerciseId = exerciseId, IsSkipped = true },
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Access denied.");
    }

    private async Task<Guid> SeedDayAsync(
        Guid ownerId,
        int completedExerciseCount = 0,
        int incompleteExerciseCount = 0,
        int skippedExerciseCount = 0,
        int partialExerciseCount = 0)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var template = new ExerciseTemplate
        {
            Name = "Back Squat",
            PrimaryMuscleGroup = MuscleGroup.Quads
        };

        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(6)
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = today,
            DayOfWeek = today.DayOfWeek,
            IsCompleted = completedExerciseCount > 0 && incompleteExerciseCount == 0 && partialExerciseCount == 0
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);

        var targetExerciseId = Guid.Empty;

        void AddExercise(bool completed, bool skipped, bool partial)
        {
            var exercise = new Exercise
            {
                ExerciseTemplateId = template.Id,
                Name = template.Name,
                PrimaryMuscleGroup = MuscleGroup.Quads,
                PlannedSets = 2,
                PlannedReps = 5,
                PlannedWeight = 100m,
                DailyWorkoutId = day.Id,
                IsCompleted = completed,
                IsSkipped = skipped
            };

            exercise.Sets.Add(new ExerciseSet
            {
                SetNumber = 1,
                PlannedReps = 5,
                PlannedWeight = 100m,
                IsCompleted = completed || partial
            });
            exercise.Sets.Add(new ExerciseSet
            {
                SetNumber = 2,
                PlannedReps = 5,
                PlannedWeight = 100m,
                IsCompleted = completed
            });

            if (!completed && (skipped || partial || targetExerciseId == Guid.Empty))
                targetExerciseId = exercise.Id;

            ctx.Exercises.Add(exercise);
        }

        for (var i = 0; i < completedExerciseCount; i++) AddExercise(completed: true, skipped: false, partial: false);
        for (var i = 0; i < incompleteExerciseCount; i++) AddExercise(completed: false, skipped: false, partial: false);
        for (var i = 0; i < skippedExerciseCount; i++) AddExercise(completed: false, skipped: true, partial: false);
        for (var i = 0; i < partialExerciseCount; i++) AddExercise(completed: false, skipped: false, partial: true);

        week.IsCompleted = day.IsCompleted;
        await ctx.SaveChangesAsync();

        return targetExerciseId;
    }
}
