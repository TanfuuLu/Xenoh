using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class CreateExerciseHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCoachUsesClientCustomTemplateForCoachPlan_CreatesExercise()
    {
        var coachId = UserId;
        var clientId = Guid.NewGuid();
        var (dailyWorkoutId, templateId) = await SeedCoachPlanAsync(coachId, clientId);

        await using var ctx = CreateContext();
        var handler = new CreateExerciseHandler(
            new DailyWorkoutRepository(ctx),
            new ExerciseRepository(ctx),
            new ExerciseTemplateRepository(ctx),
            new BodyweightRepository(ctx),
            new UserPrRepository(ctx),
            CurrentUser());

        var result = await handler.Handle(new CreateExerciseCommand
        {
            DailyWorkoutId = dailyWorkoutId,
            ExerciseTemplateId = templateId,
            PlannedSets = 3,
            PlannedReps = 10,
            PlannedWeight = 50m,
            Notes = "Client-specific variation"
        }, CancellationToken.None);

        result.ExerciseTemplateId.Should().Be(templateId);
        result.Name.Should().Be("Client Split Squat");
        result.Sets.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenCoachUsesAnotherUsersCustomTemplateForCoachPlan_Throws()
    {
        var coachId = UserId;
        var clientId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var (dailyWorkoutId, _) = await SeedCoachPlanAsync(coachId, clientId);
        var otherTemplateId = await SeedCustomTemplateAsync(otherUserId, "Other User Exercise");

        await using var ctx = CreateContext();
        var handler = new CreateExerciseHandler(
            new DailyWorkoutRepository(ctx),
            new ExerciseRepository(ctx),
            new ExerciseTemplateRepository(ctx),
            new BodyweightRepository(ctx),
            new UserPrRepository(ctx),
            CurrentUser());

        var act = () => handler.Handle(new CreateExerciseCommand
        {
            DailyWorkoutId = dailyWorkoutId,
            ExerciseTemplateId = otherTemplateId,
            PlannedSets = 3,
            PlannedReps = 10
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Exercise template not found.");
    }

    private async Task<(Guid DailyWorkoutId, Guid TemplateId)> SeedCoachPlanAsync(Guid coachId, Guid clientId)
    {
        await using var ctx = CreateContext();

        var template = new ExerciseTemplate
        {
            Name = "Client Split Squat",
            OwnerId = clientId,
            PrimaryMuscleGroup = MuscleGroup.Quads,
            ExerciseKind = ExerciseKind.Strength
        };

        var plan = new Plan
        {
            Name = "Client Plan",
            OwnerId = clientId,
            CreatedByCoachId = coachId,
            PlanType = PlanType.Coach,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(28))
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = plan.StartDate,
            EndDate = plan.StartDate.AddDays(6)
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = plan.StartDate,
            DayOfWeek = plan.StartDate.DayOfWeek
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        await ctx.SaveChangesAsync();

        return (day.Id, template.Id);
    }

    private async Task<Guid> SeedCustomTemplateAsync(Guid ownerId, string name)
    {
        await using var ctx = CreateContext();

        var template = new ExerciseTemplate
        {
            Name = name,
            OwnerId = ownerId,
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength
        };

        ctx.ExerciseTemplates.Add(template);
        await ctx.SaveChangesAsync();

        return template.Id;
    }
}
