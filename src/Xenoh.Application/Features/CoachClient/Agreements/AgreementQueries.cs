using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Agreements;

public sealed class PreviewAgreementHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<PreviewAgreementQuery, AgreementPreviewResponse>
{
    public async ValueTask<AgreementPreviewResponse> Handle(PreviewAgreementQuery request, CancellationToken ct)
    {
        if (request.Code.Length != 8) throw new InvalidOperationException("Enter the 8-character invitation code.");
        var invite = await db.CoachInviteCodes.AsNoTracking().Include(x => x.Coach).Include(x => x.Agreement)
            .SingleOrDefaultAsync(x => x.Code == request.Code.Trim().ToUpperInvariant(), ct)
            ?? throw new KeyNotFoundException("Invitation not found.");
        if (invite.IsUsed || invite.CoachingEndDate < CoachingPolicy.LocalDate(DateTime.UtcNow))
            throw new InvalidOperationException("This invitation is no longer available.");
        if (invite.CoachId == currentUser.UserId) throw new InvalidOperationException("You cannot accept your own invitation.");
        if (invite.Agreement is null) throw new InvalidOperationException("Ask your coach to publish a coaching agreement. This older code has no agreed terms.");
        return new(invite.CoachId, $"{invite.Coach.FirstName} {invite.Coach.LastName}".Trim(), invite.Agreement.Adapt<AgreementDto>());
    }
}

public sealed class GetAgreementHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetAgreementQuery, RelationshipAgreementResponse>
{
    public async ValueTask<RelationshipAgreementResponse> Handle(GetAgreementQuery request, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships.AsNoTracking().Include(x => x.Coach).Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == request.RelationshipId && (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId), ct)
            ?? throw new KeyNotFoundException("Agreement not found.");
        var agreements = await db.CoachingAgreements.AsNoTracking().Where(x => x.RelationshipId == r.Id)
            .OrderByDescending(x => x.Version).ProjectToType<AgreementDto>().ToListAsync(ct);
        var events = await db.CoachingAgreementEvents.AsNoTracking().Where(x => x.RelationshipId == r.Id)
            .OrderByDescending(x => x.OccurredAtUtc).ProjectToType<AgreementEventDto>().ToListAsync(ct);
        return new(r.Id, r.CoachId, $"{r.Coach.FirstName} {r.Coach.LastName}".Trim(), r.ClientId,
            $"{r.Client.FirstName} {r.Client.LastName}".Trim(), CoachingPolicy.DisplayStatus(r, DateTime.UtcNow),
            r.Revision, r.StartDate, r.EndDate, r.NoticeEndsAtUtc, CoachingPolicy.HasAccess(r, DateTime.UtcNow), r.AgreementId, agreements, events, r.TerminationRequestedBy == r.CoachId && r.NoticeEndsAtUtc == null
                && r.Status != Xenoh.Domain.Enums.RelationshipStatus.Ended && r.Status != Xenoh.Domain.Enums.RelationshipStatus.Expired);
    }
}

public sealed class ListAgreementsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListAgreementsQuery, IReadOnlyList<RelationshipAgreementSummary>>
{
    public async ValueTask<IReadOnlyList<RelationshipAgreementSummary>> Handle(ListAgreementsQuery request, CancellationToken ct)
    {
        var rows = await db.CoachClientRelationships.AsNoTracking().Include(x => x.Coach).Include(x => x.Client)
            .Where(x => x.ClientId == currentUser.UserId || x.CoachId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt).Skip((Math.Clamp(request.Page, 1, 10000) - 1) * 30).Take(30).ToListAsync(ct);
        return rows.Select(r => new RelationshipAgreementSummary(r.Id, $"{r.Coach.FirstName} {r.Coach.LastName}".Trim(),
            $"{r.Client.FirstName} {r.Client.LastName}".Trim(), CoachingPolicy.DisplayStatus(r, DateTime.UtcNow), r.StartDate, r.EndDate)).ToList();
    }
}

public sealed class PreviewEndingHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<PreviewEndingQuery, EndingPreviewResponse>
{
    public async ValueTask<EndingPreviewResponse> Handle(PreviewEndingQuery request, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RelationshipId
            && (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId), ct)
            ?? throw new KeyNotFoundException("Agreement not found.");
        if (currentUser.UserId != r.ClientId)
            throw new UnauthorizedAccessException("Only the client can confirm an ending. Coaches must request client approval.");
        var now = DateTime.UtcNow;
        return new(r.Revision, now, CoachingPolicy.EndingAt(r, now, request.Immediate), request.Immediate, r.AgreementId == null ? 0 : r.NoticeDays);
    }
}
