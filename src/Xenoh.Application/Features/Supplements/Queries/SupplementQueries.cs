using Mediator;

namespace Xenoh.Application.Features.Supplements.Queries;

public sealed record GetSupplementRegimensQuery(
    bool IncludeArchived = false,
    Guid? UserId = null) : IRequest<IReadOnlyList<SupplementRegimenResponse>>;

public sealed record GetSupplementDailyQuery(
    DateOnly Date,
    Guid? UserId = null) : IRequest<SupplementDailyResponse>;

public sealed record GetSupplementHistoryQuery(
    DateOnly From,
    DateOnly To,
    Guid? UserId = null) : IRequest<SupplementHistoryResponse>;
