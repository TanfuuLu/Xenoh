using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Features.TrainingDayShares;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class TrainingShareReuseTests : HandlerTestBase
{
    [Fact]
    public async Task CopyReusableShare_CopiesPrescriptionWithoutPerformance()
    {
        await using var db = CreateContext();
        var friendId = Guid.NewGuid();
        db.ApplicationUsers.AddRange(User(UserId, "Ana"), User(friendId, "Ben"));
        var (a, b) = Friendship.NormalizePair(UserId, friendId);
        db.Friendships.Add(new Friendship { UserAId = a, UserBId = b, RequesterId = UserId, AddresseeId = friendId, Status = FriendshipStatus.Accepted });
        var template = new ExerciseTemplate { Name = "Squat", PrimaryMuscleGroup = MuscleGroup.Quads };
        db.ExerciseTemplates.Add(template);
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new Plan { OwnerId = UserId, Name = "Target", StartDate = start, EndDate = start.AddDays(7), PlanType = PlanType.Self };
        var week = new WeeklyWorkout { Plan = plan, Name = "Week", WeekNumber = 1, StartDate = start, EndDate = start.AddDays(7) };
        var target = new DailyWorkout { WeeklyWorkout = week, Date = start, DayOfWeek = start.DayOfWeek };
        db.DailyWorkouts.Add(target);
        var share = new TrainingDayShare
        {
            UserId = friendId, SourceDailyWorkoutId = Guid.NewGuid(), WorkoutDate = start,
            DayOfWeek = start.DayOfWeek, DayStatus = DayStatus.Normal, ExerciseCount = 1, CompletedSets = 1,
            IsReusable = true,
            Exercises = [new TrainingDayShareExercise
            {
                ExerciseTemplateId = template.Id, Name = "Squat", PrimaryMuscleGroup = MuscleGroup.Quads,
                PlannedSets = 1, PlannedReps = 10, PlannedWeight = 50,
                Sets = [new TrainingDayShareSet { SetNumber = 1, PlannedReps = 10, PlannedWeight = 50, ActualReps = 8, ActualWeight = 60, IsCompleted = true }]
            }]
        };
        db.TrainingDayShares.Add(share);
        await db.SaveChangesAsync();
        var handler = new CopyReusableTrainingShareHandler(db, CurrentUser());

        var copied = await handler.Handle(new CopyReusableTrainingShareCommand(share.Id, target.Id), CancellationToken.None);

        copied.Should().Be(1);
        var exercise = await db.Exercises.Include(x => x.Sets).SingleAsync(x => x.DailyWorkoutId == target.Id);
        exercise.PlannedReps.Should().Be(10);
        exercise.PlannedWeight.Should().Be(50);
        exercise.Sets.Single().ActualReps.Should().BeNull();
        exercise.Sets.Single().ActualWeight.Should().BeNull();
    }

    private static ApplicationUser User(Guid id, string name) => new()
    {
        Id = id, FirstName = name, LastName = "Athlete", UserName = $"{id}@test.local", Email = $"{id}@test.local"
    };
}
