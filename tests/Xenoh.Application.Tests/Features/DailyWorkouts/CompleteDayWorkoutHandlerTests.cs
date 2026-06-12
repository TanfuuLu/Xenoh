using FluentAssertions;
using Mediator;
using Xunit;
using Xenoh.Application.Features.DailyWorkouts.Commands.CompleteDayWorkout;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.DailyWorkouts;

public sealed class CompleteDayWorkoutHandlerTests : HandlerTestBase
{
    private CompleteDayWorkoutHandler CreateHandler(ApplicationDbContext ctx) =>
        new(
            new DailyWorkoutRepository(ctx),
            new ExerciseRepository(ctx),
            new TestMediator(ctx),
            CurrentUser());

    [Fact]
    public async Task Handle_CompletesNonSkippedExercisesWithPlannedValues()
    {
        var dayId = await SeedDayAsync(UserId);

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(new CompleteDayWorkoutCommand(dayId), CancellationToken.None);

        response.Should().HaveCount(2);
        response.Single(e => !e.IsSkipped).Sets.Should().OnlyContain(s =>
            s.IsCompleted &&
            s.ActualReps == s.PlannedReps &&
            s.ActualWeight == s.PlannedWeight);

        await using var verify = CreateContext();
        var completed = verify.Exercises.Single(e => !e.IsSkipped);
        completed.IsCompleted.Should().BeTrue();
        completed.Sets.Should().OnlyContain(s => s.IsCompleted);
    }

    [Fact]
    public async Task Handle_PreservesSkippedExercises()
    {
        var dayId = await SeedDayAsync(UserId);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new CompleteDayWorkoutCommand(dayId), CancellationToken.None);

        await using var verify = CreateContext();
        var skipped = verify.Exercises.Single(e => e.IsSkipped);
        skipped.IsCompleted.Should().BeFalse();
        skipped.Sets.Should().OnlyContain(s => !s.IsCompleted);
    }

    [Fact]
    public async Task Handle_WhenNonOwner_Throws()
    {
        var dayId = await SeedDayAsync(Guid.NewGuid());

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CompleteDayWorkoutCommand(dayId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Access denied.");
    }

    private async Task<Guid> SeedDayAsync(Guid ownerId)
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
            DayOfWeek = today.DayOfWeek
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);

        ctx.Exercises.Add(CreateExercise(template, day.Id, skipped: false));
        ctx.Exercises.Add(CreateExercise(template, day.Id, skipped: true));
        await ctx.SaveChangesAsync();

        return day.Id;
    }

    private static Exercise CreateExercise(ExerciseTemplate template, Guid dayId, bool skipped)
    {
        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = MuscleGroup.Quads,
            PlannedSets = 2,
            PlannedReps = 5,
            PlannedWeight = 100m,
            DailyWorkoutId = dayId,
            IsSkipped = skipped
        };

        exercise.Sets.Add(new ExerciseSet
        {
            SetNumber = 1,
            PlannedReps = 5,
            PlannedWeight = 100m
        });
        exercise.Sets.Add(new ExerciseSet
        {
            SetNumber = 2,
            PlannedReps = 5,
            PlannedWeight = 100m
        });

        return exercise;
    }

    private sealed class TestMediator(ApplicationDbContext ctx) : IMediator
    {
        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default) =>
            Send((IRequest<TResponse>)command, cancellationToken);

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default) =>
            Send((IRequest<TResponse>)query, cancellationToken);

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is MarkSetCompleteCommand command)
            {
                var set = ctx.ExerciseSets.Single(s => s.Id == command.SetId);
                set.IsCompleted = true;
                set.ActualReps = command.ActualReps;
                set.ActualWeight = command.ActualWeight;
                set.CompletedAt = DateTime.UtcNow;
                ctx.SaveChanges();

                var exercise = ctx.Exercises.Single(e => e.Id == set.ExerciseId);
                exercise.IsCompleted = ctx.ExerciseSets
                    .Where(s => s.ExerciseId == exercise.Id)
                    .All(s => s.IsCompleted);

                ctx.SaveChanges();
                return ValueTask.FromResult((TResponse)(object)null!);
            }

            throw new NotSupportedException();
        }

        public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamQuery<TResponse> query,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamCommand<TResponse> command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            throw new NotSupportedException();

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
