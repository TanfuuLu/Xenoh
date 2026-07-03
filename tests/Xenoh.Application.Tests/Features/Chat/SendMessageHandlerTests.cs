using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Chat.Commands.SendMessage;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.Chat;

public sealed class SendMessageHandlerTests : HandlerTestBase
{
    private readonly FakeChatRealtimeService _fakeChatService = new();
    private readonly FakeNotificationService _fakeNotificationService = new();

    private SendMessageHandler CreateHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser(), _fakeNotificationService, _fakeChatService);

    [Fact]
    public async Task Handle_WhenValidRelationship_CreatesMessageAndBroadcasts()
    {
        var (relationshipId, coachId) = await SeedRelationshipAsync(clientId: UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new SendMessageCommand
        {
            RelationshipId = relationshipId,
            Content = "Hello coach!"
        }, CancellationToken.None);

        result.Content.Should().Be("Hello coach!");
        result.SenderId.Should().Be(UserId);
        result.RelationshipId.Should().Be(relationshipId);

        await using var verify = CreateContext();
        var saved = await verify.Messages.SingleOrDefaultAsync(m => m.RelationshipId == relationshipId);
        saved.Should().NotBeNull();
        saved!.Content.Should().Be("Hello coach!");

        _fakeChatService.Calls.Should().HaveCount(1);
        _fakeChatService.Calls[0].RelationshipId.Should().Be(relationshipId);
        _fakeChatService.Calls[0].RecipientIds.Should().Contain(coachId);
    }

    [Fact]
    public async Task Handle_WhenRelationshipNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new SendMessageCommand
        {
            RelationshipId = Guid.NewGuid(),
            Content = "Hello?"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a participant*");
    }

    [Fact]
    public async Task Handle_WhenRelationshipIsEnded_Throws()
    {
        var (relationshipId, _) = await SeedRelationshipAsync(clientId: UserId, status: RelationshipStatus.Ended);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new SendMessageCommand
        {
            RelationshipId = relationshipId,
            Content = "Hello?"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a participant*");
    }

    [Fact]
    public async Task Handle_WhenCallerIsCoach_MessageSaved()
    {
        var clientId = Guid.NewGuid();
        var (relationshipId, _) = await SeedRelationshipAsync(clientId: clientId, coachId: UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new SendMessageCommand
        {
            RelationshipId = relationshipId,
            Content = "Train hard!"
        }, CancellationToken.None);

        result.SenderId.Should().Be(UserId);
        result.Content.Should().Be("Train hard!");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<(Guid RelationshipId, Guid OtherPartyId)> SeedRelationshipAsync(
        Guid? clientId = null,
        Guid? coachId = null,
        RelationshipStatus status = RelationshipStatus.Active)
    {
        var actualClientId = clientId ?? Guid.NewGuid();
        var actualCoachId = coachId ?? Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();

        if (!await ctx.ApplicationUsers.AnyAsync(u => u.Id == actualClientId))
            ctx.ApplicationUsers.Add(new ApplicationUser
            {
                Id = actualClientId,
                Email = $"client_{actualClientId:N}@test.com",
                UserName = $"client_{actualClientId:N}@test.com",
                FirstName = "Client",
                LastName = "User"
            });

        if (!await ctx.ApplicationUsers.AnyAsync(u => u.Id == actualCoachId))
            ctx.ApplicationUsers.Add(new ApplicationUser
            {
                Id = actualCoachId,
                Email = $"coach_{actualCoachId:N}@test.com",
                UserName = $"coach_{actualCoachId:N}@test.com",
                FirstName = "Coach",
                LastName = "User"
            });

        var relationship = new CoachClientRelationship
        {
            ClientId = actualClientId,
            CoachId = actualCoachId,
            Status = status,
            StartDate = today
        };
        ctx.CoachClientRelationships.Add(relationship);
        await ctx.SaveChangesAsync();

        var otherParty = clientId.HasValue ? actualCoachId : actualClientId;
        return (relationship.Id, otherParty);
    }
}
