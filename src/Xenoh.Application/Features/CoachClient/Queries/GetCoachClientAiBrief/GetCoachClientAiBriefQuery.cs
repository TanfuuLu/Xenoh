using Mediator;

namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachClientAiBrief;

public sealed record GetCoachClientAiBriefQuery(Guid ClientId, string? Language)
    : IRequest<CoachClientAiBriefResponse>;

public sealed record CoachClientAiBriefResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    string Headline,
    string AttentionLevel,
    string ProgressSummary,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Opportunities,
    string SuggestedMessage
);
