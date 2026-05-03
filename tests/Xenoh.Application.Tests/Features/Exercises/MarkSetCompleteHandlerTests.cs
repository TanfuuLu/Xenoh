using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class MarkSetCompleteHandlerTests : HandlerTestBase
{
    // ─── seed helpers ────────────────────────────────────────────────────────

    private async Task<(Guid SetId, Guid ExerciseTemplateId)> SeedAsync(
        bool alreadyCompleted = false,
        Guid? ownerOverride = null)
    {
        await using var ctx = CreateContext();

        var ownerId = ownerOverride ?? UserId;

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
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6))
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DayOfWeek = DateTime.Today.DayOfWeek
        };

        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = MuscleGroup.Quads,
            PlannedSets = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            DailyWorkoutId = day.Id
        };

        var set = new ExerciseSet
        {
            ExerciseId = exercise.Id,
            SetNumber = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            IsCompleted = alreadyCompleted
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        ctx.Exercises.Add(exercise);
        ctx.ExerciseSets.Add(set);
        await ctx.SaveChangesAsync();

        return (set.Id, template.Id);
    }

    private MarkSetCompleteHandler CreateHandler(ApplicationDbContext ctx) =>
        new(
            new ExerciseSetRepository(ctx),
            new WorkoutHistoryRepository(ctx),
            new UserPrRepository(ctx),
            CurrentUser(),
            new FakeNotificationService(),
            new TestUserManager(new ApplicationUser { Id = UserId }));

    // ─── RPE tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithRpe_SavesRpeAndReturnsInResponse()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var command = new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m,
            Rpe = 8.5m
        };

        var response = await handler.Handle(command, CancellationToken.None);

        var setResponse = response.Sets.Single();
        setResponse.Rpe.Should().Be(8.5m);
        setResponse.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutRpe_RpeRemainsNullInResponse()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var command = new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m
            // Rpe not provided
        };

        var response = await handler.Handle(command, CancellationToken.None);

        response.Sets.Single().Rpe.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithRpe_PersistsToDatabase()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m,
            Rpe = 7m
        }, CancellationToken.None);

        // Verify via separate context
        await using var verifyCtx = CreateContext();
        var persisted = await verifyCtx.ExerciseSets.FindAsync(setId);
        persisted!.Rpe.Should().Be(7m);
    }

    [Fact]
    public async Task Handle_AlreadyCompleted_ReturnsCurrentState()
    {
        var (setId, _) = await SeedAsync(alreadyCompleted: true);
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var response = await handler.Handle(new MarkSetCompleteCommand { SetId = setId }, CancellationToken.None);

        response.Sets.Single().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AccessDenied_ThrowsInvalidOperationException()
    {
        var otherUserId = Guid.NewGuid();
        var (setId, _) = await SeedAsync(ownerOverride: otherUserId);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx); // CurrentUser = UserId, plan owned by otherUserId

        var act = () => handler.Handle(new MarkSetCompleteCommand { SetId = setId }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Access denied*");
    }

    [Fact]
    public async Task Handle_FirstWeightedSet_CreatesCurrentPrAndHistory()
    {
        var (setId, templateId) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m
        }, CancellationToken.None);

        await using var verifyCtx = CreateContext();
        verifyCtx.UserExercisePRs
            .Single(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .Weight.Should().Be(100m);

        verifyCtx.UserExercisePRHistories
            .Single(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .Weight.Should().Be(100m);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(100)]
    public async Task Handle_LowerOrEqualWeight_DoesNotCreateHistory(decimal completedWeight)
    {
        var (setId, templateId) = await SeedAsync();
        await using var seedCtx = CreateContext();
        seedCtx.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = UserId,
            ExerciseTemplateId = templateId,
            Weight = 100m,
            Reps = 5,
            AchievedAt = DateTime.UtcNow.AddDays(-1)
        });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = completedWeight
        }, CancellationToken.None);

        await using var verifyCtx = CreateContext();
        verifyCtx.UserExercisePRs.Single(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .Weight.Should().Be(100m);
        verifyCtx.UserExercisePRHistories
            .Where(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BreakingPr_AddsBaselineAndNewHistory()
    {
        var (setId, templateId) = await SeedAsync();
        var baselineAchievedAt = DateTime.UtcNow.AddDays(-1);
        await using var seedCtx = CreateContext();
        seedCtx.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = UserId,
            ExerciseTemplateId = templateId,
            Weight = 100m,
            Reps = 5,
            AchievedAt = baselineAchievedAt
        });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 3,
            ActualWeight = 110m
        }, CancellationToken.None);

        await using var verifyCtx = CreateContext();
        verifyCtx.UserExercisePRs.Single(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .Weight.Should().Be(110m);

        verifyCtx.UserExercisePRHistories
            .Where(p => p.UserId == UserId && p.ExerciseTemplateId == templateId)
            .OrderBy(p => p.AchievedAt)
            .Select(p => p.Weight)
            .Should().Equal(100m, 110m);
    }

    [Fact]
    public async Task GetHistoryAsync_WithCurrentPrAndNoHistory_ReturnsSyntheticBaseline()
    {
        var (_, templateId) = await SeedAsync(alreadyCompleted: true);
        await using var seedCtx = CreateContext();
        seedCtx.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = UserId,
            ExerciseTemplateId = templateId,
            Weight = 100m,
            Reps = 5,
            AchievedAt = DateTime.UtcNow
        });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var repo = new UserPrRepository(ctx);

        var history = await repo.GetHistoryAsync(UserId, templateId, CancellationToken.None);

        history.Should().ContainSingle();
        history[0].Weight.Should().Be(100m);
    }

    private sealed class TestUserManager(ApplicationUser user)
        : UserManager<ApplicationUser>(
            new TestUserStore(user),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance)
    {
        public override Task<ApplicationUser?> FindByIdAsync(string userId) =>
            Task.FromResult(user.Id.ToString() == userId ? user : null);

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user) =>
            Task.FromResult(IdentityResult.Success);
    }

    private sealed class TestUserStore(ApplicationUser user) : IUserStore<ApplicationUser>
    {
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString() == userId ? user : null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName == normalizedUserName ? user : null);

        public void Dispose()
        {
        }
    }
}
