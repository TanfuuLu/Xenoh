using FluentAssertions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class PlanAnalyticsRepositoryTests : HandlerTestBase
{
    [Fact]
    public async Task GetCoachOverviewAsync_ExcludesCoachPersonalPlans()
    {
        var coachId = UserId;
        var clientId = Guid.NewGuid();
        await using var seedCtx = CreateContext();

        var coach = new ApplicationUser
        {
            Id = coachId,
            FirstName = "Coach",
            LastName = "Owner",
            Email = "coach@example.com"
        };
        var client = new ApplicationUser
        {
            Id = clientId,
            FirstName = "Client",
            LastName = "Lifter",
            Email = "client@example.com"
        };

        seedCtx.Users.AddRange(coach, client);
        seedCtx.Plans.AddRange(
            new Plan
            {
                Name = "Coach Personal",
                OwnerId = coachId,
                PlanType = PlanType.Self,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(28))
            },
            new Plan
            {
                Name = "Client Assigned",
                OwnerId = clientId,
                CreatedByCoachId = coachId,
                PlanType = PlanType.Coach,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(28))
            });

        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetCoachOverviewAsync(coachId, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Client Assigned");
        result[0].OwnerId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithPrimaryOnlySet_UsesFullVolume()
    {
        var planId = await SeedPlanAsync(new ExerciseSeed(
            WeekNumber: 1,
            PrimaryMuscleGroup: MuscleGroup.Chest,
            SecondaryMuscleGroups: [],
            ActualReps: 10,
            ActualWeight: 100m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.MuscleGroupVolume.Should().ContainSingle();
        result.MuscleGroupVolume[0].MuscleGroup.Should().Be("Chest");
        result.MuscleGroupVolume[0].TotalVolume.Should().Be(1000m);
        result.MuscleGroupVolume[0].PrimaryVolume.Should().Be(1000m);
        result.MuscleGroupVolume[0].SecondaryVolume.Should().Be(0m);
        result.MuscleGroupVolume[0].PercentOfTotal.Should().Be(100m);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithSecondaryMuscles_UsesHalfWeightedVolume()
    {
        var planId = await SeedPlanAsync(new ExerciseSeed(
            WeekNumber: 1,
            PrimaryMuscleGroup: MuscleGroup.Quads,
            SecondaryMuscleGroups: [MuscleGroup.Glutes, MuscleGroup.Hamstrings],
            ActualReps: 5,
            ActualWeight: 100m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.MuscleGroupVolume.Single(m => m.MuscleGroup == "Quads").TotalVolume.Should().Be(500m);
        result.MuscleGroupVolume.Single(m => m.MuscleGroup == "Glutes").SecondaryVolume.Should().Be(250m);
        result.MuscleGroupVolume.Single(m => m.MuscleGroup == "Hamstrings").SecondaryVolume.Should().Be(250m);
        result.MuscleGroupVolume.Sum(m => m.PercentOfTotal).Should().Be(100m);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithZeroVolumeCompletedSet_KeepsSetCountAndZeroPercent()
    {
        var planId = await SeedPlanAsync(new ExerciseSeed(
            WeekNumber: 1,
            PrimaryMuscleGroup: MuscleGroup.Back,
            SecondaryMuscleGroups: [],
            ActualReps: null,
            ActualWeight: null));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        var back = result!.MuscleGroupVolume.Single();
        back.CompletedSets.Should().Be(1);
        back.TotalVolume.Should().Be(0m);
        back.PercentOfTotal.Should().Be(0m);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithEmptyPlan_ReturnsEmptyMuscleAnalytics()
    {
        var planId = await SeedPlanAsync();

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.MuscleGroupVolume.Should().BeEmpty();
        result.MuscleGroupHeatmap.Should().BeEmpty();
        result.MuscleGroupBalance.MaxVolume.Should().Be(0m);
        result.Insights.Should().NotBeEmpty();
        result.TrainingScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetAnalyticsAsync_IncludesTrainingScoreAndInsights()
    {
        var planId = await SeedPlanAsync(
            new ExerciseSeed(1, MuscleGroup.Chest, [], 10, 100m),
            new ExerciseSeed(2, MuscleGroup.Chest, [], 10, 70m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TrainingScore.Should().BeInRange(0, 100);
        result.Insights.Should().Contain(i => i.Type == "Recommendation");
        result.Insights.Should().Contain(i => i.Type == "VolumeTrend");
    }

    [Fact]
    public async Task GetAnalyticsAsync_WithOtherUserPlan_ReturnsNull()
    {
        var planId = await SeedPlanAsync(ownerId: Guid.NewGuid());

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAnalyticsAsync_BuildsWeeklyMuscleHeatmap()
    {
        var planId = await SeedPlanAsync(
            new ExerciseSeed(1, MuscleGroup.Chest, [], 10, 100m),
            new ExerciseSeed(2, MuscleGroup.Chest, [MuscleGroup.Triceps], 5, 80m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        var chest = result!.MuscleGroupHeatmap.Single(m => m.MuscleGroup == "Chest");
        chest.Weeks.Single(w => w.WeekNumber == 1).Volume.Should().Be(1000m);
        chest.Weeks.Single(w => w.WeekNumber == 2).Volume.Should().Be(400m);
        result.MuscleGroupHeatmap.Single(m => m.MuscleGroup == "Triceps")
            .Weeks.Single(w => w.WeekNumber == 2).Volume.Should().Be(200m);
    }

    private async Task<Guid> SeedPlanAsync(params ExerciseSeed[] exercises) =>
        await SeedPlanAsync(UserId, exercises);

    private async Task<Guid> SeedPlanAsync(Guid? ownerId = null, params ExerciseSeed[] exercises)
    {
        await using var ctx = CreateContext();
        var plan = new Plan
        {
            Name = "Analytics Plan",
            OwnerId = ownerId ?? UserId,
            PlanType = PlanType.Self,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(13))
        };

        ctx.Plans.Add(plan);

        foreach (var weekNumber in exercises.Select(e => e.WeekNumber).DefaultIfEmpty(1).Distinct())
        {
            var week = new WeeklyWorkout
            {
                PlanId = plan.Id,
                WeekNumber = weekNumber,
                Name = $"Week {weekNumber}",
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays((weekNumber - 1) * 7)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays((weekNumber - 1) * 7 + 6))
            };

            var day = new DailyWorkout
            {
                WeeklyWorkoutId = week.Id,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays((weekNumber - 1) * 7)),
                DayOfWeek = DayOfWeek.Monday,
                IsCompleted = true
            };

            ctx.WeeklyWorkouts.Add(week);
            ctx.DailyWorkouts.Add(day);

            foreach (var seed in exercises.Where(e => e.WeekNumber == weekNumber))
            {
                var template = new ExerciseTemplate
                {
                    Name = $"{seed.PrimaryMuscleGroup} Template",
                    PrimaryMuscleGroup = seed.PrimaryMuscleGroup,
                    SecondaryMuscleGroups = seed.SecondaryMuscleGroups
                };
                var exercise = new Exercise
                {
                    ExerciseTemplateId = template.Id,
                    Name = template.Name,
                    PrimaryMuscleGroup = seed.PrimaryMuscleGroup,
                    SecondaryMuscleGroups = seed.SecondaryMuscleGroups,
                    PlannedSets = 1,
                    PlannedReps = seed.ActualReps ?? 0,
                    PlannedWeight = seed.ActualWeight,
                    DailyWorkoutId = day.Id
                };
                var set = new ExerciseSet
                {
                    ExerciseId = exercise.Id,
                    SetNumber = 1,
                    PlannedReps = seed.ActualReps ?? 0,
                    PlannedWeight = seed.ActualWeight,
                    ActualReps = seed.ActualReps,
                    ActualWeight = seed.ActualWeight,
                    IsCompleted = true
                };

                ctx.ExerciseTemplates.Add(template);
                ctx.Exercises.Add(exercise);
                ctx.ExerciseSets.Add(set);
            }
        }

        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    private sealed record ExerciseSeed(
        int WeekNumber,
        MuscleGroup PrimaryMuscleGroup,
        List<MuscleGroup> SecondaryMuscleGroups,
        int? ActualReps,
        decimal? ActualWeight);
}
