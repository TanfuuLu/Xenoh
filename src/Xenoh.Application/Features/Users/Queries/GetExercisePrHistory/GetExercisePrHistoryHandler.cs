using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Users.Queries.GetExercisePrHistory;

public sealed class GetExercisePrHistoryHandler(
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisePrHistoryQuery, List<ExercisePrHistoryPointResponse>>
{
    public async ValueTask<List<ExercisePrHistoryPointResponse>> Handle(
        GetExercisePrHistoryQuery request,
        CancellationToken cancellationToken) =>
        await userPrRepo.GetHistoryAsync(currentUser.UserId, request.ExerciseTemplateId, cancellationToken);
}
