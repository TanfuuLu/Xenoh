namespace Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;

public sealed record AnalysisSection(string Headline, string Detail);

public sealed record AnalysisRecommendation(string Headline, IReadOnlyList<string> Actions);

public sealed record AnalysisContent(
    AnalysisSection TrainingAdherence,
    AnalysisSection BodyMetrics,
    AnalysisSection VolumeStrength,
    AnalysisSection MuscleBalance,
    AnalysisSection EffortGap,
    AnalysisRecommendation Recommendation
);

public sealed record UserAnalysisResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    AnalysisContent Content
);
