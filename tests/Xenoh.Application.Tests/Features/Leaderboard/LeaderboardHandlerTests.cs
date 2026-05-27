using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Leaderboard;

public sealed class LeaderboardHandlerTests : HandlerTestBase
{
    private GetLeaderboardHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new LeaderboardRepository(ctx));

    // ── Squat single-lift ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoData_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetLeaderboardQuery { Type = "squat" }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSquatPRs_ReturnsRankedByWeightDescending()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        var templateId = await SeedSquatTemplateAsync();
        await SeedUserWithPrAsync(user1, templateId, weight: 180m);
        await SeedUserWithPrAsync(user2, templateId, weight: 220m);
        await SeedUserWithPrAsync(user3, templateId, weight: 150m);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetLeaderboardQuery { Type = "squat" }, CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Score.Should().Be(220m); // heaviest first
        result[1].Score.Should().Be(180m);
        result[2].Score.Should().Be(150m);
        result[0].Rank.Should().Be(1);
        result[1].Rank.Should().Be(2);
        result[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithGenderFilter_ReturnsOnlyMatchingGender()
    {
        var maleUser   = Guid.NewGuid();
        var femaleUser = Guid.NewGuid();
        var templateId = await SeedSquatTemplateAsync();
        await SeedUserWithPrAsync(maleUser,   templateId, weight: 200m, gender: Gender.Male);
        await SeedUserWithPrAsync(femaleUser, templateId, weight: 160m, gender: Gender.Female);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetLeaderboardQuery { Type = "squat", Gender = "female" }, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(femaleUser);
    }

    [Fact]
    public async Task Handle_WithBenchType_ReturnsBenchPRs()
    {
        var userId     = Guid.NewGuid();
        var templateId = await SeedCompetitionLiftTemplateAsync(CompetitionLiftType.Bench);
        await SeedUserWithPrAsync(userId, templateId, weight: 120m);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetLeaderboardQuery { Type = "bench" }, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
        result[0].Score.Should().Be(120m);
    }

    // ── DOTS ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithDotsType_ReturnsDotsRanking()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var squatId    = await SeedCompetitionLiftTemplateAsync(CompetitionLiftType.Squat);
        var benchId    = await SeedCompetitionLiftTemplateAsync(CompetitionLiftType.Bench);
        var deadliftId = await SeedCompetitionLiftTemplateAsync(CompetitionLiftType.Deadlift);

        // user1: heavy total
        await SeedUserWithPrAsync(user1, squatId,    weight: 250m, gender: Gender.Male);
        await SeedUserWithPrAsync(user1, benchId,    weight: 180m);
        await SeedUserWithPrAsync(user1, deadliftId, weight: 300m);
        await SeedBodyweightAsync(user1, 90m);

        // user2: lighter total
        await SeedUserWithPrAsync(user2, squatId,    weight: 150m, gender: Gender.Male);
        await SeedUserWithPrAsync(user2, benchId,    weight: 100m);
        await SeedUserWithPrAsync(user2, deadliftId, weight: 180m);
        await SeedBodyweightAsync(user2, 80m);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetLeaderboardQuery { Type = "dots" }, CancellationToken.None);

        result.Should().NotBeEmpty();
        // user1 has larger total — should rank higher
        result[0].UserId.Should().Be(user1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> SeedSquatTemplateAsync()
        => await SeedCompetitionLiftTemplateAsync(CompetitionLiftType.Squat);

    private async Task<Guid> SeedCompetitionLiftTemplateAsync(CompetitionLiftType liftType)
    {
        await using var ctx = CreateContext();
        // Re-use template if already seeded for this lift type in this test class instance
        var existing = ctx.ExerciseTemplates
            .FirstOrDefault(t => t.IsCompetitionLift && t.CompetitionLiftType == liftType);
        if (existing is not null)
            return existing.Id;

        var template = new ExerciseTemplate
        {
            Name = liftType.ToString(),
            PrimaryMuscleGroup = MuscleGroup.Quads,
            ExerciseKind = ExerciseKind.Strength,
            IsCompetitionLift = true,
            CompetitionLiftType = liftType
        };
        ctx.ExerciseTemplates.Add(template);
        await ctx.SaveChangesAsync();
        return template.Id;
    }

    private async Task SeedUserWithPrAsync(Guid userId, Guid templateId, decimal weight, Gender gender = Gender.Male)
    {
        await using var ctx = CreateContext();
        if (!ctx.ApplicationUsers.Any(u => u.Id == userId))
        {
            ctx.ApplicationUsers.Add(new ApplicationUser
            {
                Id = userId,
                Email = $"user_{userId:N}@test.com",
                UserName = $"user_{userId:N}@test.com",
                FirstName = "Athlete",
                LastName = userId.ToString("N")[..6],
                Gender = gender
            });
        }
        // Upsert PR: if it already exists for this user+template, update weight
        var existing = ctx.UserExercisePRs
            .FirstOrDefault(p => p.UserId == userId && p.ExerciseTemplateId == templateId);
        if (existing is not null)
            existing.Weight = weight;
        else
            ctx.UserExercisePRs.Add(new UserExercisePR
            {
                UserId = userId,
                ExerciseTemplateId = templateId,
                Weight = weight,
                Reps = 1,
                AchievedAt = DateTime.UtcNow
            });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedBodyweightAsync(Guid userId, decimal weightKg)
    {
        await using var ctx = CreateContext();
        ctx.BodyweightLogs.Add(new BodyweightLog
        {
            UserId = userId,
            Weight = weightKg,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });
        await ctx.SaveChangesAsync();
    }
}
