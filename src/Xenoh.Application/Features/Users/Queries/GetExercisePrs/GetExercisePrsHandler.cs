using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Users.Queries.GetExercisePrs;

public sealed class GetExercisePrsHandler(
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisePrsQuery, List<ExercisePrResponse>>
{
    public async ValueTask<List<ExercisePrResponse>> Handle(
        GetExercisePrsQuery request,
        CancellationToken cancellationToken) =>
        await userPrRepo.GetExercisePrsAsync(currentUser.UserId, cancellationToken);
}
