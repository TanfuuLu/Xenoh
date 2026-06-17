using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Queries.GetUserTrainingDayShares;

public sealed record GetUserTrainingDaySharesQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<TrainingDayShareResponse>>;
