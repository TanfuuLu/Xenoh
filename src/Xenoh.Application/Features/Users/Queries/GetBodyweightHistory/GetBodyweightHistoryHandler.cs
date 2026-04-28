using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;

namespace Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;

public sealed class GetBodyweightHistoryHandler(
    IBodyweightRepository bodyweightRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetBodyweightHistoryQuery, List<BodyweightLogResponse>>
{
    public async ValueTask<List<BodyweightLogResponse>> Handle(
        GetBodyweightHistoryQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;

        if (userId != callerId)
        {
            var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(
                callerId, userId, cancellationToken);

            if (!hasRelationship)
                throw new UnauthorizedAccessException("You do not have access to this user's bodyweight history.");
        }

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        return await bodyweightRepo.GetHistoryAsync(userId, from, cancellationToken);
    }
}
