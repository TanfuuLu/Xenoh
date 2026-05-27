using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.Share;

public sealed class GetPrShareDataHandlerTests : HandlerTestBase
{
    private GetPrShareDataHandler CreateHandler(ApplicationDbContext ctx) =>
        new(ctx);

    [Fact]
    public async Task Handle_WhenAllDataExists_ReturnsShareData()
    {
        var templateId = await SeedPrAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetPrShareDataQuery(UserId, templateId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ExerciseName.Should().Be("Squat");
        result.UserName.Should().Be("John Doe");
        result.WeightKg.Should().Be(140m);
        result.Reps.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenNoPrExists_ReturnsNull()
    {
        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetPrShareDataQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsNull()
    {
        // Seed PR with a template ID that doesn't exist in the DB
        var userId = UserId;
        var missingTemplateId = Guid.NewGuid();

        await using var seed = CreateContext();
        seed.ApplicationUsers.Add(new ApplicationUser
        {
            Id = userId, Email = "u@test.com", UserName = "u@test.com",
            FirstName = "User", LastName = "A"
        });
        seed.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = userId,
            ExerciseTemplateId = missingTemplateId,
            Weight = 50m,
            Reps = 1,
            AchievedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetPrShareDataQuery(userId, missingTemplateId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNull()
    {
        var ghostUserId = Guid.NewGuid();
        var templateId  = await SeedTemplateOnlyAsync();

        await using var seed = CreateContext();
        seed.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = ghostUserId,
            ExerciseTemplateId = templateId,
            Weight = 80m,
            Reps = 5,
            AchievedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(
            new GetPrShareDataQuery(ghostUserId, templateId), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPrAsync(Guid userId)
    {
        await using var ctx = CreateContext();
        ctx.ApplicationUsers.Add(new ApplicationUser
        {
            Id = userId, Email = "athlete@test.com", UserName = "athlete@test.com",
            FirstName = "John", LastName = "Doe"
        });
        var template = new ExerciseTemplate
        {
            Name = "Squat",
            PrimaryMuscleGroup = MuscleGroup.Quads,
            ExerciseKind = ExerciseKind.Strength
        };
        ctx.ExerciseTemplates.Add(template);
        ctx.UserExercisePRs.Add(new UserExercisePR
        {
            UserId = userId,
            ExerciseTemplateId = template.Id,
            Weight = 140m,
            Reps = 3,
            AchievedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        return template.Id;
    }

    private async Task<Guid> SeedTemplateOnlyAsync()
    {
        await using var ctx = CreateContext();
        var template = new ExerciseTemplate
        {
            Name = "Bench Press",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength
        };
        ctx.ExerciseTemplates.Add(template);
        await ctx.SaveChangesAsync();
        return template.Id;
    }
}
