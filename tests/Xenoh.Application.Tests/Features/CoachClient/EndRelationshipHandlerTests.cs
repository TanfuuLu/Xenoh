using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Application.Features.Supplements;
using Xenoh.Application.Features.Supplements.Commands;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class EndRelationshipHandlerTests : IdentityHandlerTestBase
{
    private readonly Guid CoachId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Ending_AnActiveRelationship_RemovesOnlyTheCoachAuthoredRegimens()
    {
        var relationshipId = await SeedRelationshipAsync(RelationshipStatus.Active);
        await SeedRegimenAsync(authorId: CoachId);
        await SeedRegimenAsync(authorId: UserId, name: "Own vitamin");

        await using (var context = CreateContext())
        {
            await Handler(context, UserId).Handle(
                new EndRelationshipCommand { RelationshipId = relationshipId },
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        var remaining = await verify.SupplementRegimens.ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].Name.Should().Be("Own vitamin");
        remaining[0].CreatedByUserId.Should().Be(UserId);

        (await verify.CoachClientRelationships.SingleAsync()).Status
            .Should().Be(RelationshipStatus.Ended);
    }

    [Fact]
    public async Task Ending_AnActiveRelationship_RemovesOnlyTheCoachAuthoredMealPlanDays()
    {
        var relationshipId = await SeedRelationshipAsync(RelationshipStatus.Active);
        await SeedMealPlanDayAsync(Today, authorId: CoachId);
        await SeedMealPlanDayAsync(Today.AddDays(1), authorId: UserId);
        await SeedMealPlanDayAsync(Today.AddDays(2), authorId: null);

        await using (var context = CreateContext())
        {
            await Handler(context, UserId).Handle(
                new EndRelationshipCommand { RelationshipId = relationshipId },
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        var remaining = await verify.MealPlanDays.OrderBy(d => d.Date).ToListAsync();
        remaining.Should().HaveCount(2);
        remaining[0].CreatedByUserId.Should().Be(UserId);
        // Rows written before authorship tracking existed are treated as the client's own.
        remaining[1].CreatedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task Ending_APendingRequest_LeavesRegimensAlone()
    {
        var relationshipId = await SeedRelationshipAsync(RelationshipStatus.Pending);
        await SeedRegimenAsync(authorId: UserId, name: "Own vitamin");

        await using (var context = CreateContext())
        {
            await Handler(context, UserId).Handle(
                new EndRelationshipCommand { RelationshipId = relationshipId },
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        (await verify.SupplementRegimens.SingleAsync()).Name.Should().Be("Own vitamin");
    }

    private EndRelationshipHandler Handler(ApplicationDbContext context, Guid callerId) =>
        new(
            new CoachClientRepository(context),
            new PlanRepository(context),
            new SupplementRepository(context),
            context,
            new FakeCurrentUserService(callerId),
            new FakeNotificationService(),
            CreateUserManager());

    private async Task<Guid> SeedRelationshipAsync(RelationshipStatus status)
    {
        await SeedUserAsync(UserId, "client@test.local", "Passw0rd!", "Client");
        await SeedUserAsync(CoachId, "coach@test.local", "Passw0rd!", "Coach");

        await using var context = CreateContext();
        var relationship = new CoachClientRelationship
        {
            CoachId = CoachId,
            ClientId = UserId,
            Status = status,
            StartDate = Today.AddDays(-30)
        };
        context.CoachClientRelationships.Add(relationship);
        await context.SaveChangesAsync();
        return relationship.Id;
    }

    private async Task SeedMealPlanDayAsync(DateOnly date, Guid? authorId)
    {
        await using var context = CreateContext();
        context.MealPlanDays.Add(new MealPlanDay
        {
            UserId = UserId,
            Date = date,
            CreatedByUserId = authorId
        });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Goes through the create handler so the regimen carries the same schedule-version
    /// and dose-slot graph a real one would.
    /// </summary>
    private async Task SeedRegimenAsync(Guid authorId, string name = "Creatine")
    {
        await using var context = CreateContext();
        var request = new CreateSupplementRegimenRequest(
            name,
            null,
            null,
            null,
            null,
            Today,
            [new SupplementDoseSlotRequest(5, "g", new TimeOnly(8, 0), Enum.GetValues<DayOfWeek>())]);

        await new CreateSupplementRegimenHandler(
                new SupplementRepository(context),
                new CoachClientRepository(context),
                new FakeCurrentUserService(authorId))
            .Handle(new CreateSupplementRegimenCommand(request, UserId), CancellationToken.None);
    }
}
