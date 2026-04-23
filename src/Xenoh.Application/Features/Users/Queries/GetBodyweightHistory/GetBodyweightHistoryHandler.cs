using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;

namespace Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;

public sealed class GetBodyweightHistoryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetBodyweightHistoryQuery, List<BodyweightLogResponse>>
{
    public async ValueTask<List<BodyweightLogResponse>> Handle(GetBodyweightHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));

        return await context.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Date >= from)
            .OrderByDescending(b => b.Date)
            .Select(b => new BodyweightLogResponse(b.Id, b.Weight, b.Date))
            .ToListAsync(cancellationToken);
    }
}
