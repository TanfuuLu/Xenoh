using Mediator;

namespace Xenoh.Application.Features.Insights.Queries.GetPlanProgressInsight;

public sealed record GetPlanProgressInsightQuery(Guid PlanId, string? Language)
    : IRequest<PlanProgressInsightResponse>;

public sealed record PlanProgressInsightResponse(
    string Language,
    string PlanName,
    DateTime GeneratedAt,
    string Headline,
    string Trajectory,
    string Summary,
    IReadOnlyList<string> WhatsWorking,
    IReadOnlyList<string> FocusAreas,
    IReadOnlyList<string> NextBlock
);
