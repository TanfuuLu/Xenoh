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

        var codes = await db.CoachInviteCodes
            .AsNoTracking()
            .Include(c => c.Agreement)
            .Where(c => c.CoachId == coachId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        return codes.Select(GenerateInviteCodeHandler.ToDto).ToList();
    }
}
