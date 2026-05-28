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
    public async Task GetAllByOwnerAsync_CountsCompletedWeeksFromWeekState()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await using var seedCtx = CreateContext();

        seedCtx.Users.Add(new ApplicationUser
        {
            Id = UserId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        });

        var plan = new Plan
        {
            Name = "Completed Week Plan",
            OwnerId = UserId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(13)
        };

        var completedWeek = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6),
            IsCompleted = true
        };

        var completedDayOnlyWeek = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 2,
            Name = "Week 2",
            StartDate = today.AddDays(7),
            EndDate = today.AddDays(13),
            IsCompleted = false
        };

        var completedDay = new DailyWorkout
        {
            WeeklyWorkoutId = completedDayOnlyWeek.Id,
            Date = today.AddDays(7),
            DayOfWeek = today.AddDays(7).DayOfWeek,
            IsCompleted = true
        };

        var completedExercise = new Exercise
        {
            DailyWorkoutId = completedDay.Id,
            Name = "Completed Exercise",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            PlannedSets = 1,
            PlannedReps = 5,
            IsCompleted = true
        };

        seedCtx.Plans.Add(plan);
        seedCtx.WeeklyWorkouts.AddRange(completedWeek, completedDayOnlyWeek);
        seedCtx.DailyWorkouts.Add(completedDay);
        seedCtx.Exercises.Add(completedExercise);
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAllByOwnerAsync(UserId, 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].TotalWeeks.Should().Be(2);
        result.Items[0].CompletedWeeks.Should().Be(1);
        result.Items[0].CompletedDays.Should().Be(1);
    }

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
        var result = await new PlanRepository(ctx).GetCoachOverviewAsync(coachId, 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Client Assigned");
        result.Items[0].OwnerId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetMonitoringByOwnersAsync_WithNoClients_ReturnsEmpty()
    {
        await using var ctx = CreateContext();

        var result = await new PlanRepository(ctx).GetMonitoringByOwnersAsync([], DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonitoringByOwnersAsync_ReturnsActivePlanAdherence()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var seedCtx = CreateContext();
        var plan = new Plan
        {
            Name = "Client Block",
            OwnerId = UserId,
            PlanType = PlanType.Coach,
            StartDate = today.AddDays(-2),
            EndDate = today.AddDays(1)
        };
        seedCtx.Plans.Add(plan);

        for (var i = 0; i < 4; i++)
        {
            var week = new WeeklyWorkout
            {
                PlanId = plan.Id,
                WeekNumber = 1,
                Name = "Week 1",
                StartDate = today.AddDays(-2),
                EndDate = today.AddDays(4)
            };
            var day = new DailyWorkout
            {
                WeeklyWorkoutId = week.Id,
                Date = today.AddDays(i - 2),
                DayOfWeek = today.AddDays(i - 2).DayOfWeek,
                Status = i == 1 ? DayStatus.Missed : DayStatus.Normal
            };
            var exercise = new Exercise
            {
                DailyWorkoutId = day.Id,
                Name = $"Exercise {i}",
                PrimaryMuscleGroup = MuscleGroup.Chest,
                PlannedSets = 1,
                PlannedReps = 5,
                IsCompleted = i == 0
            };

            seedCtx.WeeklyWorkouts.Add(week);
            seedCtx.DailyWorkouts.Add(day);
            seedCtx.Exercises.Add(exercise);
        }

        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetMonitoringByOwnersAsync([UserId], today, CancellationToken.None);

        result.Should().ContainSingle();
        var snapshot = result[0];
        snapshot.ActivePlanId.Should().Be(plan.Id);
        snapshot.ActivePlanName.Should().Be("Client Block");
        snapshot.CompletedWorkoutDays.Should().Be(1);
        snapshot.TotalWorkoutDays.Should().Be(4);
        snapshot.MissedWorkoutDays.Should().Be(1);
        snapshot.ActivePlanProgressPercent.Should().Be(25);
        snapshot.ExpectedProgressPercent.Should().Be(75);
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
        result.CompletedSets.Should().Be(0);
        result.AvgRpe.Should().BeNull();
        result.HighRpeSets.Should().Be(0);
        result.WarningDays.Should().Be(0);
        result.TotalDurationSeconds.Should().Be(0);
        result.Insights.Should().NotBeEmpty();
        result.TrainingScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetAnalyticsAsync_ReturnsCompletedWorkSummary()
    {
        var planId = await SeedPlanAsync(
            new ExerciseSeed(
                WeekNumber: 1,
                PrimaryMuscleGroup: MuscleGroup.Chest,
                SecondaryMuscleGroups: [],
                ActualReps: 8,
                ActualWeight: 80m,
                Rpe: 8m,
                DurationSeconds: 1800),
            new ExerciseSeed(
                WeekNumber: 1,
                PrimaryMuscleGroup: MuscleGroup.Back,
                SecondaryMuscleGroups: [],
                ActualReps: 4,
                ActualWeight: 100m,
                Rpe: 9.5m,
                DurationSeconds: 1500,
                PlannedReps: 6,
                PlannedWeight: 100m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetAnalyticsAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CompletedSets.Should().Be(2);
        result.AvgRpe.Should().Be(8.8m);
        result.HighRpeSets.Should().Be(1);
        result.WarningDays.Should().Be(1);
        result.TotalDurationSeconds.Should().Be(3300);
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

    [Fact]
    public async Task GetDesignAnalysisAsync_WithBalancedPlan_ReturnsBroadCoverageAndLowRisk()
    {
        var planId = await SeedDesignPlanAsync(
            new DesignExerciseSeed(0, "Back Squat", MuscleGroup.Quads, [MuscleGroup.Glutes], 4, 5, 100m),
            new DesignExerciseSeed(2, "Bench Press", MuscleGroup.Chest, [MuscleGroup.Triceps], 4, 6, 80m),
            new DesignExerciseSeed(2, "Pull Up", MuscleGroup.Back, [MuscleGroup.Biceps], 4, 8, null),
            new DesignExerciseSeed(4, "Romanian Deadlift", MuscleGroup.Hamstrings, [MuscleGroup.Glutes], 3, 8, 90m),
            new DesignExerciseSeed(4, "Overhead Press", MuscleGroup.Shoulders, [MuscleGroup.Triceps], 3, 6, 50m),
            new DesignExerciseSeed(6, "Barbell Row", MuscleGroup.Back, [MuscleGroup.Biceps], 4, 8, 70m),
            new DesignExerciseSeed(6, "Plank", MuscleGroup.Abs, [], 3, 45, null));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetDesignAnalysisAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Structure.PlannedTrainingDays.Should().Be(4);
        result.Structure.PlannedRestDays.Should().Be(3);
        result.Workload.PlannedSets.Should().Be(25);
        result.Workload.PlannedTonnage.Should().Be(9220m);
        result.MovementPatterns.Where(p => p.Pattern != "Carry/Cardio/Isolation")
            .Should().OnlyContain(p => p.IsCovered);
        result.RecoveryRisks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDesignAnalysisAsync_WithUpperBodyBias_FindsDominantAndMissingLowerCoverage()
    {
        var planId = await SeedDesignPlanAsync(
            new DesignExerciseSeed(0, "Bench Press", MuscleGroup.Chest, [MuscleGroup.Triceps], 5, 5, 100m),
            new DesignExerciseSeed(1, "Incline Bench Press", MuscleGroup.Chest, [MuscleGroup.Shoulders], 5, 8, 80m),
            new DesignExerciseSeed(3, "Cable Fly", MuscleGroup.Chest, [], 4, 12, 30m),
            new DesignExerciseSeed(5, "Triceps Pushdown", MuscleGroup.Triceps, [], 4, 12, 25m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetDesignAnalysisAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Balance.DominantMuscleGroups.Should().Contain("Chest");
        result.Balance.UndertrainedMajorMuscleGroups.Should().Contain(["Quads", "Hamstrings", "Glutes"]);
        result.MovementPatterns.Single(p => p.Pattern == "Squat/Lunge").IsCovered.Should().BeFalse();
        result.MovementPatterns.Single(p => p.Pattern == "Hinge").IsCovered.Should().BeFalse();
    }

    [Fact]
    public async Task GetDesignAnalysisAsync_WithDenseConsecutiveTrainingDays_ReturnsRecoveryWarnings()
    {
        var planId = await SeedDesignPlanAsync(
            new DesignExerciseSeed(0, "Back Squat", MuscleGroup.Quads, [MuscleGroup.Glutes], 5, 5, 120m),
            new DesignExerciseSeed(1, "Deadlift", MuscleGroup.Back, [MuscleGroup.Hamstrings], 5, 3, 150m),
            new DesignExerciseSeed(2, "Front Squat", MuscleGroup.Quads, [MuscleGroup.Glutes], 5, 5, 100m),
            new DesignExerciseSeed(3, "Leg Press", MuscleGroup.Quads, [MuscleGroup.Glutes], 12, 10, 150m));

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetDesignAnalysisAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Structure.LongestTrainingStreak.Should().Be(4);
        result.RecoveryRisks.Should().Contain(r => r.Type == "LowerBodySpacing" && r.Severity == "High");
        result.RecoveryRisks.Should().Contain(r => r.Type == "RepeatedMuscle");
        result.RecoveryRisks.Should().Contain(r => r.Type == "RestDistribution" && r.Severity == "High");
        result.RecoveryRisks.Should().Contain(r => r.Type == "DenseDay");
    }

    [Fact]
    public async Task GetDesignAnalysisAsync_WithEmptyPlan_ReturnsZeroStats()
    {
        var planId = await SeedDesignPlanAsync();

        await using var ctx = CreateContext();
        var result = await new PlanRepository(ctx).GetDesignAnalysisAsync(planId, UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Structure.TotalWeeks.Should().Be(1);
        result.Structure.PlannedTrainingDays.Should().Be(0);
        result.Structure.PlannedRestDays.Should().Be(7);
        result.Workload.PlannedExercises.Should().Be(0);
        result.Workload.PlannedSets.Should().Be(0);
        result.MuscleGroups.Should().BeEmpty();
        result.RecoveryRisks.Should().BeEmpty();
        result.Variety.UniqueExercises.Should().Be(0);
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
                    PlannedReps = seed.PlannedReps ?? seed.ActualReps ?? 0,
                    PlannedWeight = seed.PlannedWeight ?? seed.ActualWeight,
                    DailyWorkoutId = day.Id,
                    IsCompleted = true,
                    DurationSeconds = seed.DurationSeconds
                };
                var set = new ExerciseSet
                {
                    ExerciseId = exercise.Id,
                    SetNumber = 1,
                    PlannedReps = seed.PlannedReps ?? seed.ActualReps ?? 0,
                    PlannedWeight = seed.PlannedWeight ?? seed.ActualWeight,
                    ActualReps = seed.ActualReps,
                    ActualWeight = seed.ActualWeight,
                    Rpe = seed.Rpe,
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

    private async Task<Guid> SeedDesignPlanAsync(params DesignExerciseSeed[] exercises)
    {
        await using var ctx = CreateContext();
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var plan = new Plan
        {
            Name = "Design Plan",
            OwnerId = UserId,
            PlanType = PlanType.Self,
            StartDate = startDate,
            EndDate = startDate.AddDays(6)
        };
        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = startDate,
            EndDate = startDate.AddDays(6)
        };

        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);

        var exerciseGroups = exercises.GroupBy(e => e.DayOffset).ToDictionary(g => g.Key, g => g.ToList());
        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var dayExercises = exerciseGroups.GetValueOrDefault(dayOffset) ?? [];
            var date = startDate.AddDays(dayOffset);
            var day = new DailyWorkout
            {
                WeeklyWorkoutId = week.Id,
                Date = date,
                DayOfWeek = date.DayOfWeek,
                Status = dayExercises.Count == 0 ? DayStatus.Rest : DayStatus.Normal
            };
            ctx.DailyWorkouts.Add(day);

            foreach (var seed in dayExercises)
            {
                var template = new ExerciseTemplate
                {
                    Name = seed.Name,
                    PrimaryMuscleGroup = seed.PrimaryMuscleGroup,
                    SecondaryMuscleGroups = seed.SecondaryMuscleGroups
                };
                var exercise = new Exercise
                {
                    ExerciseTemplateId = template.Id,
                    Name = seed.Name,
                    PrimaryMuscleGroup = seed.PrimaryMuscleGroup,
                    SecondaryMuscleGroups = seed.SecondaryMuscleGroups,
                    PlannedSets = seed.PlannedSets,
                    PlannedReps = seed.PlannedReps,
                    PlannedWeight = seed.PlannedWeight,
                    DailyWorkoutId = day.Id
                };

                ctx.ExerciseTemplates.Add(template);
                ctx.Exercises.Add(exercise);
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
        decimal? ActualWeight,
        decimal? Rpe = null,
        int? DurationSeconds = null,
        int? PlannedReps = null,
        decimal? PlannedWeight = null);

    private sealed record DesignExerciseSeed(
        int DayOffset,
        string Name,
        MuscleGroup PrimaryMuscleGroup,
        List<MuscleGroup> SecondaryMuscleGroups,
        int PlannedSets,
        int PlannedReps,
        decimal? PlannedWeight);
}
