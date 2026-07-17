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
        db.Friendships.Add(new Friendship { UserAId = a, UserBId = b, RequesterId = UserId, AddresseeId = friendId, Status = FriendshipStatus.Accepted });
        await db.SaveChangesAsync();
        var notifications = new FakeNotificationService();
        var handler = new CreateFitnessChallengeHandler(db, CurrentUser(), notifications);

        var result = await handler.Handle(new CreateFitnessChallengeCommand
        {
            Title = "Four week consistency",
            TargetSessionsPerWeek = 3,
            StartsOn = NextMonday(),
            DurationWeeks = 4,
            InviteeUserIds = [friendId]
        }, CancellationToken.None);

        result.Members.Should().HaveCount(2);
        result.Members.Should().Contain(x => x.UserId == UserId && x.Status == "Accepted");
        result.Members.Should().Contain(x => x.UserId == friendId && x.Status == "Invited");
        notifications.Calls.Should().ContainSingle(x => x.RecipientId == friendId && x.Type == "FitnessChallengeInvite");
    }

    [Fact]
    public async Task CreateChallenge_ForStranger_IsRejected()
    {
        await using var db = CreateContext();
        var strangerId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(strangerId, "Cam"));
        await db.SaveChangesAsync();
        var handler = new CreateFitnessChallengeHandler(db, CurrentUser(), new FakeNotificationService());

        var action = () => handler.Handle(new CreateFitnessChallengeCommand
        {
            Title = "Private goal", TargetSessionsPerWeek = 2, StartsOn = NextMonday(), DurationWeeks = 2,
            InviteeUserIds = [strangerId]
        }, CancellationToken.None).AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*accepted friends or active clients*");
    }

    [Fact]
    public async Task ChallengeProgress_CountsDistinctCompletedWorkoutDates()
    {
        await using var db = CreateContext();
        var start = NextMonday();
        var user = User(UserId, "Ana");
        db.ApplicationUsers.Add(user);
        var challenge = new FitnessChallenge
        {
            CreatorId = UserId, Title = "Consistency", TargetSessionsPerWeek = 3,
            StartsOn = start, EndsOn = start.AddDays(6),
            Members = [new FitnessChallengeMember { UserId = UserId, Status = FitnessChallengeMemberStatus.Accepted }]
        };
        var plan = new Plan { OwnerId = UserId, Name = "Plan", StartDate = start, EndDate = start.AddDays(6), PlanType = PlanType.Self };
        var week = new WeeklyWorkout { Plan = plan, WeekNumber = 1, Name = "Week", StartDate = start, EndDate = start.AddDays(6) };
        db.DailyWorkouts.AddRange(
            new DailyWorkout { WeeklyWorkout = week, Date = start, DayOfWeek = DayOfWeek.Monday, IsCompleted = true },
            new DailyWorkout { WeeklyWorkout = week, Date = start, DayOfWeek = DayOfWeek.Monday, IsCompleted = true },
            new DailyWorkout { WeeklyWorkout = week, Date = start.AddDays(1), DayOfWeek = DayOfWeek.Tuesday, IsCompleted = false });
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync();
        var handler = new GetFitnessChallengeHandler(db, CurrentUser());

        var result = await handler.Handle(new GetFitnessChallengeQuery(challenge.Id), CancellationToken.None);

        result.Members.Single().CompletedSessions.Should().Be(1);
        result.Members.Single().TargetSessions.Should().Be(3);
    }

    private static DateOnly NextMonday()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        do { date = date.AddDays(1); } while (date.DayOfWeek != DayOfWeek.Monday);
        return date;
    }

    private static ApplicationUser User(Guid id, string name) => new()
    {
        Id = id, FirstName = name, LastName = "Athlete", UserName = $"{id}@test.local", Email = $"{id}@test.local"
    };
}
