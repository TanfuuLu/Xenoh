using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyInviteCodes;

public sealed class GetMyInviteCodesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyInviteCodesQuery, List<CoachInviteCodeDto>>
{
    public async ValueTask<List<CoachInviteCodeDto>> Handle(
        GetMyInviteCodesQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        return await db.CoachInviteCodes
            .AsNoTracking()
            .Where(c => c.CoachId == coachId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CoachInviteCodeDto(
                c.Id,
                c.Code,
                c.CoachingStartDate,
                c.CoachingEndDate,
                c.IsUsed,
                c.UsedByClientId,
                c.UsedAt,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
