using Mediator;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleInsight;

public sealed record GetCycleInsightQuery(string Language) : IRequest<CycleInsightResponse>;
