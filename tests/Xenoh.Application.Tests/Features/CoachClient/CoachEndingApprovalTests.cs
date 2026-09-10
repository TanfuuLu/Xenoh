using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Agreements;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class CoachEndingApprovalTests : IdentityHandlerTestBase
{
    private readonly Guid coachId = Guid.NewGuid();

    [Fact]
    public async Task Client_approval_starts_the_agreed_notice_period()
    {
        var relationship = await SeedPendingCoachRequestAsync();
        await using var db = CreateContext();
        var endHandler = EndHandler(db, UserId);
        var handler = new RespondToCoachEndingHandler(
            db, new FakeCurrentUserService(UserId), new EndingMediator(endHandler), new FakeNotificationService());

        await handler.Handle(new(relationship.Id, relationship.Revision, true), default);

        var updated = await db.CoachClientRelationships.SingleAsync();
        updated.Status.Should().Be(RelationshipStatus.PendingTermination);
        updated.TerminationRequestedBy.Should().Be(UserId);
        updated.NoticeEndsAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        (await db.CoachingAgreementEvents.CountAsync(x => x.Kind == "EndingRequested")).Should().Be(1);
    }

    [Fact]
    public async Task Client_decline_keeps_coaching_active()
    {
        var relationship = await SeedPendingCoachRequestAsync();
        await using var db = CreateContext();
        var handler = new RespondToCoachEndingHandler(
            db, new FakeCurrentUserService(UserId), new EndingMediator(EndHandler(db, UserId)), new FakeNotificationService());

        await handler.Handle(new(relationship.Id, relationship.Revision, false), default);

        var updated = await db.CoachClientRelationships.SingleAsync();
        updated.Status.Should().Be(RelationshipStatus.Active);
        updated.TerminationRequestedBy.Should().BeNull();
        updated.NoticeEndsAtUtc.Should().BeNull();
        (await db.CoachingAgreementEvents.CountAsync(x => x.Kind == "EndingApprovalDeclined")).Should().Be(1);
    }

    [Fact]
    public async Task Coach_cannot_approve_their_own_request()
    {
        var relationship = await SeedPendingCoachRequestAsync();
        await using var db = CreateContext();
        var handler = new RespondToCoachEndingHandler(
            db, new FakeCurrentUserService(coachId), new EndingMediator(EndHandler(db, coachId)), new FakeNotificationService());

        var act = async () => await handler.Handle(new(relationship.Id, relationship.Revision, true), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var preview = async () => await new PreviewEndingHandler(db, new FakeCurrentUserService(coachId))
            .Handle(new(relationship.Id), default);
        await preview.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private async Task<CoachClientRelationship> SeedPendingCoachRequestAsync()
    {
        await SeedUserAsync(UserId, "client-ending@test.local", "Passw0rd!", "Client");
        await SeedUserAsync(coachId, "coach-ending@test.local", "Passw0rd!", "Coach");
        await using var db = CreateContext();
        var relationship = new CoachClientRelationship
        {
            CoachId = coachId,
            ClientId = UserId,
            AgreementId = Guid.NewGuid(),
            NoticeDays = 7,
            Status = RelationshipStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            TerminationRequestedBy = coachId
        };
        db.CoachClientRelationships.Add(relationship);
        await db.SaveChangesAsync();
        return relationship;
    }

    private EndRelationshipHandler EndHandler(ApplicationDbContext db, Guid callerId) => new(
        new CoachClientRepository(db), new PlanRepository(db), new SupplementRepository(db), db,
        new FakeCurrentUserService(callerId), new FakeNotificationService(), CreateUserManager());

    private sealed class EndingMediator(EndRelationshipHandler handler) : IMediator
    {
        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default) =>
            Send((IRequest<TResponse>)command, ct);

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default) =>
            Send((IRequest<TResponse>)query, ct);

        public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            if (request is EndRelationshipCommand command)
            {
                await handler.Handle(command, ct);
                return (TResponse)(object)Unit.Value;
            }
            throw new NotSupportedException();
        }

        public ValueTask<object?> Send(object message, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken ct = default) => throw new NotSupportedException();
        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification => throw new NotSupportedException();
        public ValueTask Publish(object notification, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
