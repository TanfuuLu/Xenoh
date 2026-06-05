using Mediator;

namespace Xenoh.Application.Features.Insights.Queries.GetTrainingCoachTip;

public sealed record GetTrainingCoachTipQuery(string? Language) : IRequest<TrainingCoachTipResponse>;

public sealed record TrainingCoachTipResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    string Headline,
    string Category,
    string Insight,
    IReadOnlyList<string> Evidence,
    string WhyItMatters,
    string NextAction,
    string Confidence
);
