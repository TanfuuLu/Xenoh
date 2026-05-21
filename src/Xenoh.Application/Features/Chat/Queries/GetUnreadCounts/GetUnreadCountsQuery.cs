using Mediator;

namespace Xenoh.Application.Features.Chat.Queries.GetUnreadCounts;

public sealed record GetUnreadCountsQuery : IRequest<IReadOnlyDictionary<Guid, int>>;
