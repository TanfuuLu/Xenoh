using Mediator;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

namespace Xenoh.Application.Features.CoachClient.Queries.GetClientPowerlifting;

public sealed record GetClientPowerliftingQuery(Guid ClientId) : IRequest<ClientPowerliftingResponse>;

public sealed record ClientPowerliftingResponse(
    Guid ClientId,
    PowerliftingSection? Powerlifting,
    List<TrainingInsightResponse> Insights);
