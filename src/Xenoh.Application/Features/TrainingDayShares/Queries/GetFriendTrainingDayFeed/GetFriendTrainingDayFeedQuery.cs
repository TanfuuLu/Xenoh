using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Queries.GetFriendTrainingDayFeed;

public sealed record GetFriendTrainingDayFeedQuery(int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<TrainingDayShareResponse>>;
