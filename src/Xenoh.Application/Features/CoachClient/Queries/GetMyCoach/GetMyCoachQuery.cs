using Mediator;
using Xenoh.Application.Features.CoachClient;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;

public sealed record GetMyCoachQuery : IRequest<CoachRelationshipResponse?>;
