using FluentAssertions;
using Xenoh.Application.Features.FitnessChallenges;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class FitnessChallengeTests : HandlerTestBase
{
    [Fact]
    public async Task CreateChallenge_ForAcceptedFriend_CreatesPrivateInvitation()
    {
        await using var db = CreateContext();
        var friendId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(friendId, "Ben"));
        var (a, b) = Friendship.NormalizePair(UserId, friendId);
        db.Friendships.Add(new Friendship
        {
            UserAId = a,
            UserBId = b,
            RequesterId = UserId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Accepted
        });
        await db.SaveChangesAsync();
        var notifications = new FakeNotificationService();
        var handler = new CreateFitnessChallengeHandler(db, CurrentUser(), notifications);

        var result = await handler.Handle(
            new CreateFitnessChallengeCommand(Input(inviteeIds: [friendId])),
            CancellationToken.None);

        result.Members.Should().HaveCount(2);
        result.Members.Should().Contain(x => x.UserId == UserId && x.Status == "Accepted");
        result.Members.Should().Contain(x => x.UserId == friendId && x.Status == "Invited");
        notifications.Calls.Should().ContainSingle(x =>
            x.RecipientId == friendId && x.Type == "FitnessChallengeInvite");
    }

    [Fact]
    public async Task CreateChallenge_ForStranger_IsRejected()
    {
        await using var db = CreateContext();
        var strangerId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(strangerId, "Cam"));
        await db.SaveChangesAsync();
        var handler = new CreateFitnessChallengeHandler(
            db,
            CurrentUser(),
            new FakeNotificationService());

        var action = () => handler.Handle(
            new CreateFitnessChallengeCommand(Input(inviteeIds: [strangerId])),
            CancellationToken.None).AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*accepted friends or active coach/client connections*");
    }

    [Fact]
    public async Task ChallengeProgress_CountsDistinctCompletedWorkoutDates()
    {
        await using var db = CreateContext();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var startUtc = DateTime.UtcNow.AddDays(-1);
        var user = User(UserId, "Ana");
        db.ApplicationUsers.Add(user);
        var challenge = new FitnessChallenge
        {
            CreatorId = UserId,
            Title = "Consistency",
            MetricType = FitnessChallengeMetricType.TrainingSessions,
            TargetSessionsPerWeek = 3,
            Capacity = 10,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartsAtUtc = startUtc,
            EndsAtUtc = startUtc.AddDays(7),
            Members =
            [
                new FitnessChallengeMember
                {
                    UserId = UserId,
                    Status = FitnessChallengeMemberStatus.Accepted
                }
            ]
        };
        var plan = new Plan
        {
            OwnerId = UserId,
            Name = "Plan",
            StartDate = startDate,
            EndDate = startDate.AddDays(6),
            PlanType = PlanType.Self
        };
        var week = new WeeklyWorkout
        {
            Plan = plan,
            WeekNumber = 1,
            Name = "Week",
            StartDate = startDate,
            EndDate = startDate.AddDays(6)
        };
        db.DailyWorkouts.AddRange(
            new DailyWorkout
            {
                WeeklyWorkout = week,
                Date = startDate,
                DayOfWeek = DayOfWeek.Monday,
                IsCompleted = true
            },
            new DailyWorkout
            {
                WeeklyWorkout = week,
                Date = startDate,
                DayOfWeek = DayOfWeek.Monday,
                IsCompleted = true
            },
            new DailyWorkout
            {
                WeeklyWorkout = week,
                Date = startDate.AddDays(1),
                DayOfWeek = DayOfWeek.Tuesday,
                IsCompleted = false
            });
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new GetFitnessChallengeHandler(db, CurrentUser());

        var result = await handler.Handle(
            new GetFitnessChallengeQuery(challenge.Id),
            CancellationToken.None);

        result.Members.Single().CompletedSessions.Should().Be(1);
        result.Members.Single().Score.Should().Be(1);
        result.Members.Single().TargetSessions.Should().BePositive();
    }

    [Fact]
    public async Task CustomCheckIn_AllowsOnlyOnePerChallengeLocalDay()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var challenge = ActiveChallenge(FitnessChallengeMetricType.CustomCheckIns);
        challenge.CheckInPrompt = "Complete mobility work";
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new CheckInFitnessChallengeHandler(db, CurrentUser());

        var first = await handler.Handle(
            new CheckInFitnessChallengeCommand(challenge.Id, "Done"),
            CancellationToken.None);
        var second = () => handler.Handle(
            new CheckInFitnessChallengeCommand(challenge.Id, null),
            CancellationToken.None).AsTask();

        first.Members.Single().Score.Should().Be(1);
        first.Members.Single().CheckedInToday.Should().BeTrue();
        await second.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already checked in today*");
    }

    [Fact]
    public async Task CustomCheckIn_OnEndLocalDate_IsRejected()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var challenge = ActiveChallenge(FitnessChallengeMetricType.CustomCheckIns);
        challenge.TimeZoneId = "UTC";
        challenge.StartsAtUtc = now.Date.AddDays(-1);
        challenge.EndsAtUtc = now.Date.AddDays(1).AddTicks(-1);
        challenge.CheckInPrompt = "Complete mobility work";
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new CheckInFitnessChallengeHandler(db, CurrentUser());

        var action = () => handler.Handle(
            new CheckInFitnessChallengeCommand(challenge.Id, "Done"),
            CancellationToken.None).AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*local date range*");
    }

    [Fact]
    public async Task JoinChallenge_AfterStart_IsRejected()
    {
        await using var db = CreateContext();
        var creatorId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(creatorId, "Ben"));
        var challenge = ActiveChallenge(FitnessChallengeMetricType.TrainingStreak, creatorId);
        challenge.AccessType = FitnessChallengeAccessType.Community;
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new JoinFitnessChallengeHandler(
            db,
            CurrentUser(),
            new FakeNotificationService());

        var action = () => handler.Handle(
            new JoinFitnessChallengeCommand(challenge.Id),
            CancellationToken.None).AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Enrollment is closed*");
    }

    [Fact]
    public async Task SbdImprovement_UsesPreStartE1RmBaseline()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var template = new ExerciseTemplate
        {
            Name = "Squat",
            IsCompetitionLift = true,
            CompetitionLiftType = CompetitionLiftType.Squat
        };
        db.ExerciseTemplates.Add(template);
        var challenge = ActiveChallenge(FitnessChallengeMetricType.SbdImprovement);
        challenge.SelectedLifts = [CompetitionLiftType.Squat];
        db.FitnessChallenges.Add(challenge);
        db.UserExercisePRHistories.AddRange(
            new UserExercisePRHistory
            {
                UserId = UserId,
                ExerciseTemplateId = template.Id,
                Weight = 100,
                Reps = 1,
                AchievedAt = challenge.StartsAtUtc.AddDays(-1)
            },
            new UserExercisePRHistory
            {
                UserId = UserId,
                ExerciseTemplateId = template.Id,
                Weight = 110,
                Reps = 1,
                AchievedAt = challenge.StartsAtUtc.AddHours(2)
            });
        await db.SaveChangesAsync();
        var handler = new GetFitnessChallengeHandler(db, CurrentUser());

        var result = await handler.Handle(
            new GetFitnessChallengeQuery(challenge.Id),
            CancellationToken.None);

        result.Members.Single().BaselineReady.Should().BeTrue();
        result.Members.Single().Score.Should().Be(10);
    }

    [Fact]
    public void ValidateInput_WithUndefinedMetricType_IsRejected()
    {
        var input = Input() with
        {
            MetricType = (FitnessChallengeMetricType)999
        };

        var action = () => FitnessChallengeRules.ValidateInput(
            input,
            DateTime.UtcNow,
            maxWeeks: 12,
            maxMembers: 25);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*metric type*");
    }

    [Fact]
    public void ValidateInput_WithUndefinedAccessType_IsRejected()
    {
        var input = Input() with
        {
            AccessType = (FitnessChallengeAccessType)999
        };

        var action = () => FitnessChallengeRules.ValidateInput(
            input,
            DateTime.UtcNow,
            maxWeeks: 12,
            maxMembers: 25);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*access type*");
    }

    [Fact]
    public void ValidateInput_WithUndefinedSelectedLift_IsRejected()
    {
        var input = Input() with
        {
            MetricType = FitnessChallengeMetricType.SbdImprovement,
            SelectedLifts = [(CompetitionLiftType)999]
        };

        var action = () => FitnessChallengeRules.ValidateInput(
            input,
            DateTime.UtcNow,
            maxWeeks: 12,
            maxMembers: 25);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*competition lift*");
    }

    [Fact]
    public void ValidateInput_WithLocalSchedule_ConvertsUsingSelectedTimeZone()
    {
        var input = Input() with
        {
            TimeZoneId = "Asia/Bangkok",
            StartsAtLocal = "2026-08-01T09:30",
            EndsAtLocal = "2026-08-15T09:30"
        };

        var result = FitnessChallengeRules.ValidateInput(
            input,
            new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc),
            maxWeeks: 12,
            maxMembers: 25);

        result.StartsAtUtc.Should().Be(
            new DateTime(2026, 8, 1, 2, 30, 0, DateTimeKind.Utc));
        result.EndsAtUtc.Should().Be(
            new DateTime(2026, 8, 15, 2, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ValidateInput_WithInvalidDaylightSavingTime_IsRejected()
    {
        var input = Input() with
        {
            TimeZoneId = "America/New_York",
            StartsAtLocal = "2026-03-08T02:30",
            EndsAtLocal = "2026-03-15T02:30"
        };

        var action = () => FitnessChallengeRules.ValidateInput(
            input,
            new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            maxWeeks: 12,
            maxMembers: 25);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not exist*timezone*");
    }

    [Fact]
    public void ValidateInput_WithAmbiguousDaylightSavingTime_IsRejected()
    {
        var input = Input() with
        {
            TimeZoneId = "America/New_York",
            StartsAtLocal = "2026-11-01T01:30",
            EndsAtLocal = "2026-11-08T01:30"
        };

        var action = () => FitnessChallengeRules.ValidateInput(
            input,
            new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            maxWeeks: 12,
            maxMembers: 25);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ambiguous*timezone*");
    }

    [Theory]
    [InlineData(7, 1, 3)]
    [InlineData(8, 2, 1)]
    [InlineData(10, 2, 2)]
    [InlineData(14, 2, 3)]
    public async Task ChallengeProgress_ProratesOnlyTheFinalPartialWeek(
        int durationDays,
        int expectedWeekCount,
        int expectedFinalTarget)
    {
        await using var db = CreateContext();
        var start = DateTime.UtcNow.Date.AddDays(-30);
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var challenge = new FitnessChallenge
        {
            CreatorId = UserId,
            Title = "Consistency",
            MetricType = FitnessChallengeMetricType.TrainingSessions,
            AccessType = FitnessChallengeAccessType.InviteOnly,
            TargetSessionsPerWeek = 3,
            Capacity = 10,
            TimeZoneId = "UTC",
            StartsAtUtc = start,
            EndsAtUtc = start.AddDays(durationDays),
            Members =
            [
                new FitnessChallengeMember
                {
                    UserId = UserId,
                    Status = FitnessChallengeMemberStatus.Accepted
                }
            ]
        };
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new GetFitnessChallengeHandler(db, CurrentUser());

        var result = await handler.Handle(
            new GetFitnessChallengeQuery(challenge.Id),
            CancellationToken.None);

        var weeks = result.Members.Single().Weeks;
        weeks.Should().HaveCount(expectedWeekCount);
        weeks[^1].TargetSessions.Should().Be(expectedFinalTarget);
    }

    [Fact]
    public async Task ChallengeProgress_ExcludesWorkoutOnEndLocalDate()
    {
        await using var db = CreateContext();
        var startUtc = DateTime.UtcNow.Date.AddDays(-30);
        var startDate = DateOnly.FromDateTime(startUtc);
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var challenge = new FitnessChallenge
        {
            CreatorId = UserId,
            Title = "One week",
            MetricType = FitnessChallengeMetricType.TrainingSessions,
            AccessType = FitnessChallengeAccessType.InviteOnly,
            TargetSessionsPerWeek = 3,
            Capacity = 10,
            TimeZoneId = "UTC",
            StartsAtUtc = startUtc,
            EndsAtUtc = startUtc.AddDays(7),
            Members =
            [
                new FitnessChallengeMember
                {
                    UserId = UserId,
                    Status = FitnessChallengeMemberStatus.Accepted
                }
            ]
        };
        var plan = new Plan
        {
            OwnerId = UserId,
            Name = "Plan",
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            PlanType = PlanType.Self
        };
        var week = new WeeklyWorkout
        {
            Plan = plan,
            WeekNumber = 1,
            Name = "Week",
            StartDate = startDate,
            EndDate = startDate.AddDays(7)
        };
        db.DailyWorkouts.AddRange(
            new DailyWorkout
            {
                WeeklyWorkout = week,
                Date = startDate.AddDays(6),
                DayOfWeek = DayOfWeek.Sunday,
                IsCompleted = true
            },
            new DailyWorkout
            {
                WeeklyWorkout = week,
                Date = startDate.AddDays(7),
                DayOfWeek = DayOfWeek.Monday,
                IsCompleted = true
            });
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new GetFitnessChallengeHandler(db, CurrentUser());

        var result = await handler.Handle(
            new GetFitnessChallengeQuery(challenge.Id),
            CancellationToken.None);

        result.Members.Single().CompletedSessions.Should().Be(1);
    }

    private static FitnessChallengeInput Input(IReadOnlyList<Guid>? inviteeIds = null)
    {
        var startsAt = DateTime.UtcNow.AddDays(2);
        return new FitnessChallengeInput
        {
            Title = "Four week consistency",
            Description = "Train together.",
            MetricType = FitnessChallengeMetricType.TrainingSessions,
            AccessType = FitnessChallengeAccessType.InviteOnly,
            TargetSessionsPerWeek = 3,
            Capacity = 10,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartsAtUtc = startsAt,
            EndsAtUtc = startsAt.AddDays(14),
            InviteeUserIds = inviteeIds ?? []
        };
    }

    private FitnessChallenge ActiveChallenge(
        FitnessChallengeMetricType metricType,
        Guid? creatorId = null)
    {
        var ownerId = creatorId ?? UserId;
        return new FitnessChallenge
        {
            CreatorId = ownerId,
            Title = "Challenge",
            Description = "Description",
            MetricType = metricType,
            AccessType = FitnessChallengeAccessType.InviteOnly,
            TargetSessionsPerWeek = metricType == FitnessChallengeMetricType.TrainingSessions ? 3 : 0,
            Capacity = 10,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(6),
            Members =
            [
                new FitnessChallengeMember
                {
                    UserId = ownerId,
                    Status = FitnessChallengeMemberStatus.Accepted
                }
            ]
        };
    }

    private static ApplicationUser User(Guid id, string name) => new()
    {
        Id = id,
        FirstName = name,
        LastName = "Athlete",
        UserName = $"{id}@test.local",
        Email = $"{id}@test.local"
    };
}
