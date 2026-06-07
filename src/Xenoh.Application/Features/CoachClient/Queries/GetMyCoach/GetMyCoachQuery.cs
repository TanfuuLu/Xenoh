using Mediator;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;

public sealed record GetMyCoachQuery : IRequest<CoachRelationshipResponse?>;
