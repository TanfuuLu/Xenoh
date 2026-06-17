using FluentAssertions;
using Xenoh.Application.Features.Community.Queries.GetCommunityUserProfile;
using Xenoh.Application.Features.Community.Queries.SearchCommunityUsers;
using Xenoh.Application.Features.Friends.Commands.SendFriendRequest;
using Xenoh.Application.Features.TrainingDayShares.Commands.LoveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Commands.ShareTrainingDay;
using Xenoh.Application.Features.TrainingDayShares.Commands.UnloveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Queries.GetFriendTrainingDayFeed;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class FriendCommunityTests : HandlerTestBase
{
    [Fact]
    public async Task SendFriendRequest_CreatesPendingRequestAndNotification()
    {
        await using var db = CreateContext();
        var targetId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(targetId, "Ben"));
        await db.SaveChangesAsync();
        var notifications = new FakeNotificationService();
        var handler = new SendFriendRequestHandler(db, CurrentUser(), notifications);

        var result = await handler.Handle(new SendFriendRequestCommand { TargetUserId = targetId }, CancellationToken.None);

        result.Status.Should().Be("Pending");
        result.Direction.Should().Be("outgoing");
        db.Friendships.Should().ContainSingle(f =>
            f.RequesterId == UserId &&
            f.AddresseeId == targetId &&
            f.Status == FriendshipStatus.Pending);
        notifications.Calls.Should().ContainSingle(c =>
            c.RecipientId == targetId &&
            c.Type == "FriendRequestReceived");
    }

    [Fact]
    public async Task SendFriendRequest_AutoAcceptsOppositePendingRequest()
    {
        await using var db = CreateContext();
        var otherId = Guid.NewGuid();
        var (userAId, userBId) = Friendship.NormalizePair(UserId, otherId);
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(otherId, "Ben"));
        db.Friendships.Add(new Friendship
        {
            UserAId = userAId,
            UserBId = userBId,
            RequesterId = otherId,
            AddresseeId = UserId,
            Status = FriendshipStatus.Pending
        });
        await db.SaveChangesAsync();
        var notifications = new FakeNotificationService();
        var handler = new SendFriendRequestHandler(db, CurrentUser(), notifications);

        var result = await handler.Handle(new SendFriendRequestCommand { TargetUserId = otherId }, CancellationToken.None);

        result.Status.Should().Be("Accepted");
        db.Friendships.Should().ContainSingle(f =>
            f.UserAId == userAId &&
            f.UserBId == userBId &&
            f.Status == FriendshipStatus.Accepted &&
            f.RespondedAt != null);
        notifications.Calls.Should().ContainSingle(c =>
            c.RecipientId == otherId &&
            c.Type == "FriendRequestAccepted");
    }

    [Fact]
    public async Task SearchCommunityUsers_ExcludesBlockedUsers()
    {
        await using var db = CreateContext();
        var blockedId = Guid.NewGuid();
        var visibleId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(blockedId, "Searchable"), User(visibleId, "Searchable"));
        db.UserBlocks.Add(new UserBlock { BlockerId = UserId, BlockedId = blockedId });
        await db.SaveChangesAsync();
        var handler = new SearchCommunityUsersHandler(db, CurrentUser());

        var result = await handler.Handle(new SearchCommunityUsersQuery("searchable"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(visibleId);
    }

    [Fact]
    public async Task TrainingDayShareFeed_ReturnsAcceptedFriendAndOwnShares()
    {
        await using var db = CreateContext();
        var friendId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(friendId, "Ben"), User(strangerId, "Cam"));
        var (friendA, friendB) = Friendship.NormalizePair(UserId, friendId);
        db.Friendships.Add(new Friendship
        {
            UserAId = friendA,
            UserBId = friendB,
            RequesterId = UserId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });
        db.TrainingDayShares.Add(new TrainingDayShare
        {
            UserId = friendId,
            User = db.ApplicationUsers.Local.First(u => u.Id == friendId),
            SourceDailyWorkoutId = Guid.NewGuid(),
            WorkoutDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DayOfWeek.Monday,
            DayStatus = DayStatus.Normal,
            ExerciseCount = 1,
            CompletedSets = 1,
            TotalVolume = 100,
            TotalDurationSeconds = 600
        });
        db.TrainingDayShares.Add(new TrainingDayShare
        {
            UserId = UserId,
            User = db.ApplicationUsers.Local.First(u => u.Id == UserId),
            SourceDailyWorkoutId = Guid.NewGuid(),
            WorkoutDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DayOfWeek.Wednesday,
            DayStatus = DayStatus.Normal,
            ExerciseCount = 1,
            CompletedSets = 1,
            TotalVolume = 150,
            TotalDurationSeconds = 600
        });
        db.TrainingDayShares.Add(new TrainingDayShare
        {
            UserId = strangerId,
            User = db.ApplicationUsers.Local.First(u => u.Id == strangerId),
            SourceDailyWorkoutId = Guid.NewGuid(),
            WorkoutDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DayOfWeek.Tuesday,
            DayStatus = DayStatus.Normal,
            ExerciseCount = 1,
            CompletedSets = 1,
            TotalVolume = 200,
            TotalDurationSeconds = 600
        });
        await db.SaveChangesAsync();
        var handler = new GetFriendTrainingDayFeedHandler(db, CurrentUser());

        var result = await handler.Handle(new GetFriendTrainingDayFeedQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(s => s.UserId == friendId);
        result.Should().Contain(s => s.UserId == UserId);
        result.Should().NotContain(s => s.UserId == strangerId);
    }

    [Fact]
    public async Task ShareTrainingDay_RequiresCompletedOwnedWorkout()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId, "Ana"));
        var squatTemplate = CompetitionTemplate("Squat", CompetitionLiftType.Squat, MuscleGroup.Quads);
        db.ExerciseTemplates.Add(squatTemplate);
        db.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = UserId,
            ExerciseTemplateId = squatTemplate.Id,
            Weight = 100,
            Reps = 5,
            AchievedAt = DateTime.UtcNow
        });
        var plan = new Plan
        {
            OwnerId = UserId,
            Name = "Plan",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            PlanType = PlanType.Self
        };
        var week = new WeeklyWorkout
        {
            Plan = plan,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = plan.StartDate,
            EndDate = plan.EndDate
        };
        var day = new DailyWorkout
        {
            WeeklyWorkout = week,
            Date = plan.StartDate,
            DayOfWeek = DayOfWeek.Monday,
            IsCompleted = true,
            Exercises =
            [
                new Exercise
                {
                    Name = "Squat",
                    ExerciseTemplate = squatTemplate,
                    PrimaryMuscleGroup = MuscleGroup.Quads,
                    ExerciseKind = ExerciseKind.Strength,
                    PlannedSets = 1,
                    PlannedReps = 5,
                    SortOrder = 1,
                    IsCompleted = true,
                    Sets =
                    [
                        new ExerciseSet
                        {
                            SetNumber = 1,
                            PlannedReps = 5,
                            PlannedWeight = 100,
                            ActualReps = 5,
                            ActualWeight = 100,
                            Rpe = 8,
                            IsCompleted = true
                        }
                    ]
                }
            ]
        };
        db.Plans.Add(plan);
        db.WeeklyWorkouts.Add(week);
        db.DailyWorkouts.Add(day);
        await db.SaveChangesAsync();
        var handler = new ShareTrainingDayHandler(db, CurrentUser(), new FakeNotificationService());

        var result = await handler.Handle(new ShareTrainingDayCommand { DailyWorkoutId = day.Id, Caption = "Felt strong today." }, CancellationToken.None);

        result.ExerciseCount.Should().Be(1);
        result.CompletedSets.Should().Be(1);
        result.TotalVolume.Should().Be(500);
        result.HasPersonalRecord.Should().BeTrue();
        result.Caption.Should().Be("Felt strong today.");
        result.LoveCount.Should().Be(0);
        result.LovedByCurrentUser.Should().BeFalse();
        result.Exercises.Should().ContainSingle(e => e.Name == "Squat" && e.IsPersonalRecord);
        db.TrainingDayShares.Should().ContainSingle(s =>
            s.SourceDailyWorkoutId == day.Id &&
            s.HasPersonalRecord &&
            s.Caption == "Felt strong today.");
    }

    [Fact]
    public async Task LoveTrainingDayShare_TogglesLoveForVisibleFriendShare()
    {
        await using var db = CreateContext();
        var friendId = Guid.NewGuid();
        var (userAId, userBId) = Friendship.NormalizePair(UserId, friendId);
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(friendId, "Ben"));
        db.Friendships.Add(new Friendship
        {
            UserAId = userAId,
            UserBId = userBId,
            RequesterId = UserId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });
        var share = new TrainingDayShare
        {
            UserId = friendId,
            User = db.ApplicationUsers.Local.First(u => u.Id == friendId),
            SourceDailyWorkoutId = Guid.NewGuid(),
            WorkoutDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DayOfWeek = DayOfWeek.Monday,
            DayStatus = DayStatus.Normal,
            ExerciseCount = 1,
            CompletedSets = 1,
            TotalVolume = 100,
            TotalDurationSeconds = 600,
            Caption = "Good pulls."
        };
        db.TrainingDayShares.Add(share);
        await db.SaveChangesAsync();

        var loveHandler = new LoveTrainingDayShareHandler(db, CurrentUser());
        var loved = await loveHandler.Handle(new LoveTrainingDayShareCommand(share.Id), CancellationToken.None);
        var duplicateLoved = await loveHandler.Handle(new LoveTrainingDayShareCommand(share.Id), CancellationToken.None);

        loved.LoveCount.Should().Be(1);
        loved.LovedByCurrentUser.Should().BeTrue();
        duplicateLoved.LoveCount.Should().Be(1);
        db.TrainingDayShareLoves.Should().ContainSingle(l => l.TrainingDayShareId == share.Id && l.UserId == UserId);

        var unloveHandler = new UnloveTrainingDayShareHandler(db, CurrentUser());
        var unloved = await unloveHandler.Handle(new UnloveTrainingDayShareCommand(share.Id), CancellationToken.None);

        unloved.LoveCount.Should().Be(0);
        unloved.LovedByCurrentUser.Should().BeFalse();
        db.TrainingDayShareLoves.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommunityUserProfile_ForAcceptedFriend_ReturnsTrainingTotalsAndBig3()
    {
        await using var db = CreateContext();
        var friendId = Guid.NewGuid();
        var (userAId, userBId) = Friendship.NormalizePair(UserId, friendId);
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(friendId, "Ben"));
        db.Friendships.Add(new Friendship
        {
            UserAId = userAId,
            UserBId = userBId,
            RequesterId = UserId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });

        var squatTemplate = CompetitionTemplate("Squat", CompetitionLiftType.Squat, MuscleGroup.Quads);
        var benchTemplate = CompetitionTemplate("Bench Press", CompetitionLiftType.Bench, MuscleGroup.Chest);
        var deadliftTemplate = CompetitionTemplate("Deadlift", CompetitionLiftType.Deadlift, MuscleGroup.Back);
        db.ExerciseTemplates.AddRange(squatTemplate, benchTemplate, deadliftTemplate);
        db.UserExercisePRs.AddRange(
            new UserExercisePR { UserId = friendId, ExerciseTemplateId = squatTemplate.Id, Weight = 180, Reps = 1, AchievedAt = DateTime.UtcNow },
            new UserExercisePR { UserId = friendId, ExerciseTemplateId = benchTemplate.Id, Weight = 120, Reps = 1, AchievedAt = DateTime.UtcNow },
            new UserExercisePR { UserId = friendId, ExerciseTemplateId = deadliftTemplate.Id, Weight = 220, Reps = 1, AchievedAt = DateTime.UtcNow });

        var plan = new Plan
        {
            OwnerId = friendId,
            Name = "Plan",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            PlanType = PlanType.Self
        };
        var week = new WeeklyWorkout
        {
            Plan = plan,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = plan.StartDate,
            EndDate = plan.EndDate
        };
        var day = new DailyWorkout
        {
            WeeklyWorkout = week,
            Date = plan.StartDate,
            DayOfWeek = DayOfWeek.Monday,
            IsCompleted = true,
            Exercises =
            [
                new Exercise
                {
                    Name = "Squat",
                    ExerciseTemplate = squatTemplate,
                    PrimaryMuscleGroup = MuscleGroup.Quads,
                    ExerciseKind = ExerciseKind.Strength,
                    PlannedSets = 1,
                    PlannedReps = 5,
                    SortOrder = 1,
                    IsCompleted = true,
                    DurationSeconds = 900,
                    Sets =
                    [
                        new ExerciseSet
                        {
                            SetNumber = 1,
                            PlannedReps = 5,
                            PlannedWeight = 100,
                            ActualReps = 5,
                            ActualWeight = 100,
                            IsCompleted = true
                        }
                    ]
                },
                new Exercise
                {
                    Name = "Bench Press",
                    ExerciseTemplate = benchTemplate,
                    PrimaryMuscleGroup = MuscleGroup.Chest,
                    ExerciseKind = ExerciseKind.Strength,
                    PlannedSets = 1,
                    PlannedReps = 8,
                    SortOrder = 2,
                    IsCompleted = true,
                    DurationSeconds = 600,
                    Sets =
                    [
                        new ExerciseSet
                        {
                            SetNumber = 1,
                            PlannedReps = 8,
                            PlannedWeight = 80,
                            ActualReps = 8,
                            ActualWeight = 80,
                            IsCompleted = true
                        }
                    ]
                }
            ]
        };
        db.Plans.Add(plan);
        db.WeeklyWorkouts.Add(week);
        db.DailyWorkouts.Add(day);
        await db.SaveChangesAsync();

        var handler = new GetCommunityUserProfileHandler(
            db,
            new WorkoutHistoryRepository(db),
            new BodyweightRepository(db),
            new UserPrRepository(db),
            CurrentUser());

        var result = await handler.Handle(new GetCommunityUserProfileQuery(friendId), CancellationToken.None);

        result.TotalTrainingDurationSeconds.Should().Be(1500);
        result.TotalTrainingVolume.Should().Be(1140);
        result.Big3Prs.Squat.Should().Be(180);
        result.Big3Prs.Bench.Should().Be(120);
        result.Big3Prs.Deadlift.Should().Be(220);
        result.Big3Total.Should().Be(520);
    }

    private static ApplicationUser User(Guid id, string firstName) =>
        new()
        {
            Id = id,
            FirstName = firstName,
            LastName = "User",
            Email = $"{firstName.ToLowerInvariant()}@example.com",
            UserName = $"{firstName.ToLowerInvariant()}@example.com"
        };

    private static ExerciseTemplate CompetitionTemplate(string name, CompetitionLiftType liftType, MuscleGroup muscleGroup) =>
        new()
        {
            Name = name,
            PrimaryMuscleGroup = muscleGroup,
            IsCompetitionLift = true,
            CompetitionLiftType = liftType
        };
}
