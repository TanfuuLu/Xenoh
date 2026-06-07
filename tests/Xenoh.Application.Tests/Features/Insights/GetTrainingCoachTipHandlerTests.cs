using FluentAssertions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Queries.GetTrainingCoachTip;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class GetTrainingCoachTipHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WhenSnapshotUnchanged_ReturnsCachedTip()
    {
        await using var db = CreateContext();
        SeedUser(db);

        var ai = new StubUserAnalysisAi();
        var handler = new GetTrainingCoachTipHandler(db, CurrentUser(), ai);

        var first = await handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None);
        var second = await handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None);

        first.Cached.Should().BeFalse();
        second.Cached.Should().BeTrue();
        ai.TrainingCoachTipCallCount.Should().Be(1);
        second.Category.Should().Be("Adherence");
    }

    [Fact]
    public async Task Handle_WhenSnapshotChanges_RegeneratesTip()
    {
        await using var db = CreateContext();
        SeedUser(db);

        var ai = new StubUserAnalysisAi();
        var handler = new GetTrainingCoachTipHandler(db, CurrentUser(), ai);

        await handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None);

        SeedCompletedTrainingDay(db, DateOnly.FromDateTime(DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None);

        result.Cached.Should().BeFalse();
        ai.TrainingCoachTipCallCount.Should().Be(2);
        ai.TrainingCoachTipRequests.Last().SnapshotJson.Should().Contain("\"completedSets\":1");
    }

    [Fact]
    public async Task Handle_WithSparseData_SendsSparseSnapshotAndReturnsSafeLoggingTip()
    {
        await using var db = CreateContext();
        SeedUser(db);

        var ai = new StubUserAnalysisAi(SparseTipJson);
        var handler = new GetTrainingCoachTipHandler(db, CurrentUser(), ai);

        var result = await handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None);

        result.Confidence.Should().Be("Low");
        result.NextAction.Should().Contain("Log your next workout");
        ai.TrainingCoachTipRequests.Should().ContainSingle();
        ai.TrainingCoachTipRequests.Single().SnapshotJson.Should().Contain("\"isSparse\":true");
    }

    [Fact]
    public async Task Handle_WhenAiReturnsInvalidJson_ThrowsControlledError()
    {
        await using var db = CreateContext();
        SeedUser(db);

        var ai = new StubUserAnalysisAi("not-json");
        var handler = new GetTrainingCoachTipHandler(db, CurrentUser(), ai);

        var act = () => handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI returned malformed JSON.");
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws()
    {
        await using var db = CreateContext();
        var ai = new StubUserAnalysisAi();
        var handler = new GetTrainingCoachTipHandler(
            db,
            new FakeCurrentUserService(Guid.Empty),
            ai);

        var act = () => handler.Handle(new GetTrainingCoachTipQuery("en"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not authenticated.");
        ai.TrainingCoachTipCallCount.Should().Be(0);
    }

    private void SeedUser(Xenoh.Infrastructure.Persistence.ApplicationDbContext db)
    {
        db.ApplicationUsers.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = "athlete@xenoh.app",
            Email = "athlete@xenoh.app",
            DevelopmentDirection = DevelopmentDirection.Strength,
            TrainingDiscipline = TrainingDiscipline.Powerlifting
        });
        db.SaveChanges();
    }

    private void SeedCompletedTrainingDay(
        Xenoh.Infrastructure.Persistence.ApplicationDbContext db,
        DateOnly date)
    {
        var plan = new Plan
        {
            OwnerId = UserId,
            Name = "Strength Block",
            PlanType = PlanType.Self,
            IsActive = true,
            StartDate = date.AddDays(-3),
            EndDate = date.AddDays(24)
        };

        var week = new WeeklyWorkout
        {
            Plan = plan,
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = date.AddDays(-3),
            EndDate = date.AddDays(3)
        };

        var day = new DailyWorkout
        {
            WeeklyWorkout = week,
            WeeklyWorkoutId = week.Id,
            Date = date,
            DayOfWeek = date.DayOfWeek,
            IsCompleted = true
        };

        var exercise = new Exercise
        {
            DailyWorkout = day,
            DailyWorkoutId = day.Id,
            ExerciseTemplateId = Guid.NewGuid(),
            Name = "Squat",
            PrimaryMuscleGroup = MuscleGroup.Quads,
            PlannedSets = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            IsCompleted = true
        };

        exercise.Sets.Add(new ExerciseSet
        {
            Exercise = exercise,
            ExerciseId = exercise.Id,
            SetNumber = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            ActualReps = 5,
            ActualWeight = 100m,
            Rpe = 7m,
            IsCompleted = true
        });

        day.Exercises.Add(exercise);
        week.DailyWorkouts.Add(day);
        plan.WeeklyWorkouts.Add(week);
        db.Plans.Add(plan);
    }

    private const string DefaultTipJson = """
    {
      "headline": "Stabilize the week first",
      "category": "Adherence",
      "insight": "Your current data is limited, so the first win is consistent logging.",
      "evidence": ["No completed sets in the 28-day snapshot."],
      "whyItMatters": "Reliable logs make training advice more specific.",
      "nextAction": "Log your next workout with reps, load, and RPE.",
      "confidence": "Low"
    }
    """;

    private const string SparseTipJson = """
    {
      "headline": "Start with better logs",
      "category": "General",
      "insight": "Xenoh Coach needs more completed training data before making a stronger recommendation.",
      "evidence": ["No active plan or completed sets are available."],
      "whyItMatters": "Specific coaching depends on seeing exercises, loads, reps, and effort.",
      "nextAction": "Log your next workout with reps, weight, and RPE.",
      "confidence": "Low"
    }
    """;

    private sealed class StubUserAnalysisAi(string trainingCoachTipJson = DefaultTipJson) : IUserAnalysisAi
    {
        public int TrainingCoachTipCallCount { get; private set; }
        public List<TrainingCoachTipAiRequest> TrainingCoachTipRequests { get; } = [];

        public Task<TrainingCoachTipAiResult> GenerateTrainingCoachTipAsync(
            TrainingCoachTipAiRequest request,
            CancellationToken cancellationToken)
        {
            TrainingCoachTipCallCount++;
            TrainingCoachTipRequests.Add(request);
            return Task.FromResult(new TrainingCoachTipAiResult(trainingCoachTipJson));
        }

        public Task<UserAnalysisAiResult> GenerateAsync(UserAnalysisAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<StarterPlanAiResult> GenerateStarterPlanAsync(StarterPlanAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(PlanBalanceAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(CoachClientBriefAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<CoachChatAiResult> ChatAsync(CoachChatAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
