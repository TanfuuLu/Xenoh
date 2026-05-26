using Mediator;
using Xenoh.Application.Features.CoachClient;

namespace Xenoh.Application.Features.CoachClient.Queries.GetPendingRequests;

public sealed record GetPendingRequestsQuery : IRequest<List<CoachRelationshipResponse>>;
