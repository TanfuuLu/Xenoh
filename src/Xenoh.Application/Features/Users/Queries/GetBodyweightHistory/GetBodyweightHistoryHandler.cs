using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;

namespace Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;

public sealed class GetBodyweightHistoryHandler(
    IBodyweightRepository bodyweightRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetBodyweightHistoryQuery, List<BodyweightLogResponse>>
{
    public async ValueTask<List<BodyweightLogResponse>> Handle(
        GetBodyweightHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        return await bodyweightRepo.GetHistoryAsync(userId, from, cancellationToken);
    }
}
