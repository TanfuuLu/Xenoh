using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class GetUserAnalysisMetricsTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WithSparseData_ReturnsEmptyMetricsWithoutCrashing()
    {
        await using var db = CreateContext();
        var handler = CreateHandler(db);

        var result = await handler.Handle(new GetUserAnalysisQuery("en"), CancellationToken.None);

        result.Metrics.Adherence.PlanCompletionPercent.Should().Be(0);
        result.Metrics.Bodyweight.Points.Should().BeEmpty();
        result.Metrics.Volume.RecentSetCount.Should().Be(0);
        result.Metrics.MuscleBalance.Should().BeEmpty();
        result.Metrics.EffortGap.HighRpeMisses.Should().BeEmpty();
        result.Metrics.RecentPrs.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTrainingData_ReturnsChartReadyMetrics()
    {
        await using var db = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var previousWeekStart = today.AddDays(-7);
        var currentWeekStart = today;

        var plan = new Plan
        {
            OwnerId = UserId,
            Name = "Strength Block",
            PlanType = PlanType.Self,
            IsActive = true,
            StartDate = previousWeekStart,
            EndDate = today.AddDays(5)
        };
        db.Plans.Add(plan);

        var previousWeek = CreateWeek(plan, 1, previousWeekStart, previousWeekStart.AddDays(6), completedDays: 1);
        var currentWeek = CreateWeek(plan, 2, currentWeekStart, currentWeekStart.AddDays(6), completedDays: 2);
        db.WeeklyWorkouts.AddRange(previousWeek, currentWeek);

        var previousDay = previousWeek.DailyWorkouts.First();
        var currentDay = currentWeek.DailyWorkouts.First();
        AddCompletedExercise(db, previousDay, "Bench Press", MuscleGroup.Chest, plannedReps: 5, plannedWeight: 80m, actualReps: 5, actualWeight: 80m, rpe: 7m);
        AddCompletedExercise(db, currentDay, "Squat", MuscleGroup.Quads, plannedReps: 5, plannedWeight: 100m, actualReps: 3, actualWeight: 100m, rpe: 9m);
        AddCompletedExercise(db, currentDay, "Row", MuscleGroup.Back, plannedReps: 8, plannedWeight: 60m, actualReps: 8, actualWeight: 65m, rpe: 7m);

        db.BodyweightLogs.AddRange(
            new BodyweightLog { UserId = UserId, Date = today, Weight = 82m },
            new BodyweightLog { UserId = UserId, Date = today.AddDays(-2), Weight = 81m });

        var template = new ExerciseTemplate { Name = "Deadlift", PrimaryMuscleGroup = MuscleGroup.Back };
        db.ExerciseTemplates.Add(template);
        db.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = UserId,
            ExerciseTemplateId = template.Id,
            Weight = 150m,
            Reps = 3,
            AchievedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetUserAnalysisQuery("en"), CancellationToken.None);

        result.Metrics.Adherence.ActivePlanName.Should().Be("Strength Block");
        result.Metrics.Adherence.PlanCompletionPercent.Should().Be(38);
        result.Metrics.Adherence.CurrentWeek!.CompletionPercent.Should().Be(50);
        result.Metrics.Adherence.PreviousWeek!.CompletionPercent.Should().Be(25);
        result.Metrics.Bodyweight.Points.Select(p => p.Date).Should().BeInAscendingOrder();
        result.Metrics.Bodyweight.Delta.Should().Be(1m);
        result.Metrics.Volume.CurrentWeekVolume.Should().Be(820m);
        result.Metrics.Volume.PreviousWeekVolume.Should().Be(400m);
        result.Metrics.MuscleBalance.Should().Contain(p => p.Muscle == "Quads" && p.Volume == 300m);
        result.Metrics.EffortGap.HighRpeMisses.Should().Contain(p => p.Exercise == "Squat");
        result.Metrics.EffortGap.LowRpeWins.Should().Contain(p => p.Exercise == "Row");
        result.Metrics.RecentPrs.Should().ContainSingle(p => p.Exercise == "Deadlift" && p.Weight == 150m);
    }

    private GetUserAnalysisHandler CreateHandler(Xenoh.Infrastructure.Persistence.ApplicationDbContext db) =>
        new(
            db,
            CurrentUser(),
            CreateUserManager(new ApplicationUser { Id = UserId, UserName = "test@xenoh.app", Email = "test@xenoh.app" }),
            new StubUserAnalysisAi());

    private static WeeklyWorkout CreateWeek(Plan plan, int weekNumber, DateOnly start, DateOnly end, int completedDays)
    {
        var week = new WeeklyWorkout
        {
            Plan = plan,
            PlanId = plan.Id,
            WeekNumber = weekNumber,
            Name = $"Week {weekNumber}",
            StartDate = start,
            EndDate = end
        };

        for (var i = 0; i < 4; i++)
        {
            week.DailyWorkouts.Add(new DailyWorkout
            {
                WeeklyWorkout = week,
                WeeklyWorkoutId = week.Id,
                Date = start.AddDays(i),
                DayOfWeek = start.AddDays(i).DayOfWeek,
                IsCompleted = i < completedDays
            });
        }

        return week;
    }

    private static void AddCompletedExercise(
        Xenoh.Infrastructure.Persistence.ApplicationDbContext db,
        DailyWorkout day,
        string name,
        MuscleGroup muscle,
        int plannedReps,
        decimal plannedWeight,
        int actualReps,
        decimal actualWeight,
        decimal rpe)
    {
        var exercise = new Exercise
        {
            DailyWorkout = day,
            DailyWorkoutId = day.Id,
            Name = name,
            PrimaryMuscleGroup = muscle,
            PlannedSets = 1,
            PlannedReps = plannedReps,
            PlannedWeight = plannedWeight,
            ExerciseTemplateId = Guid.NewGuid(),
            IsCompleted = true
        };

        exercise.Sets.Add(new ExerciseSet
        {
            Exercise = exercise,
            ExerciseId = exercise.Id,
            SetNumber = 1,
            PlannedReps = plannedReps,
            PlannedWeight = plannedWeight,
            ActualReps = actualReps,
            ActualWeight = actualWeight,
            Rpe = rpe,
            IsCompleted = true
        });

        db.Exercises.Add(exercise);
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationUser user) =>
        new(
            new StubUserStore(user),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

    private sealed class StubUserAnalysisAi : IUserAnalysisAi
    {
        private const string AnalysisJson = """
        {
          "trainingAdherence": { "headline": "Adherence", "detail": "Training detail." },
          "bodyMetrics": { "headline": "Body", "detail": "Body detail." },
          "volumeStrength": { "headline": "Volume", "detail": "Volume detail." },
          "muscleBalance": { "headline": "Balance", "detail": "Balance detail." },
          "effortGap": { "headline": "Effort", "detail": "Effort detail." },
          "recommendation": { "headline": "Next", "actions": ["Log consistently"] }
        }
        """;

        public Task<UserAnalysisAiResult> GenerateAsync(UserAnalysisAiRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new UserAnalysisAiResult(AnalysisJson));

        public Task<PlanProgressInsightAiResult> GeneratePlanProgressInsightAsync(PlanProgressInsightAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<StarterPlanAiResult> GenerateStarterPlanAsync(StarterPlanAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(PlanBalanceAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(CoachClientBriefAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TrainingCoachTipAiResult> GenerateTrainingCoachTipAsync(TrainingCoachTipAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<CoachChatAiResult> ChatAsync(CoachChatAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class StubUserStore(ApplicationUser user) : IUserStore<ApplicationUser>
    {
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString() == userId ? user : null);

        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(applicationUser.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(applicationUser.UserName);

        public Task SetUserNameAsync(ApplicationUser applicationUser, string? userName, CancellationToken cancellationToken)
        {
            applicationUser.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(applicationUser.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser applicationUser, string? normalizedName, CancellationToken cancellationToken)
        {
            applicationUser.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser applicationUser, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);
    }
}
