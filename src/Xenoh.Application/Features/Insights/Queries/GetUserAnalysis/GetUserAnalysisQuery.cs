using Mediator;

namespace Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;

public sealed record GetUserAnalysisQuery(string Language) : IRequest<UserAnalysisResponse>;
