using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyClients;

public sealed class GetMyClientsHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyClientsQuery, List<ClientResponse>>
{
    public async ValueTask<List<ClientResponse>> Handle(GetMyClientsQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        return await context.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Client)
            .Where(r => r.CoachId == coachId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ClientResponse(
                r.Id,
                r.ClientId,
                $"{r.Client.FirstName} {r.Client.LastName}",
                r.Client.Email!,
                r.Status.ToString(),
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
