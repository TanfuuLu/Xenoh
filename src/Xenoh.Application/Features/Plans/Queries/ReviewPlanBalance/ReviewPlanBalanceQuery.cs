using Mediator;

namespace Xenoh.Application.Features.Plans.Queries.ReviewPlanBalance;

public sealed record ReviewPlanBalanceQuery(Guid PlanId, string? Language) : IRequest<PlanBalanceReviewResponse>;

public sealed record PlanBalanceReviewResponse(
    string Headline,
    string Severity,
    string Summary,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Suggestions
);
