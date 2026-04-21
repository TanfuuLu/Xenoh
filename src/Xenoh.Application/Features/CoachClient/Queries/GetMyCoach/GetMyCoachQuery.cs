using Mediator;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;

public sealed record GetMyCoachQuery : IRequest<CoachRelationshipResponse?>;
