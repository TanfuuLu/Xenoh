namespace Xenoh.Application.Common.Interfaces;

public sealed record UserAnalysisAiRequest(
    string Language,
    string Snapshot
);

public sealed record UserAnalysisAiResult(
    string Json
);

public sealed record StarterPlanAiRequest(
    string Language,
    string QuestionnaireJson,
    string ExerciseCatalogJson
);

public sealed record StarterPlanAiResult(
    string Json
);

public sealed record PlanBalanceAiRequest(
    string Language,
    string PlanSnapshotJson
);

public sealed record PlanBalanceAiResult(
    string Json
);

public sealed record WorkoutGuidanceAiRequest(
    string Language,
    string SnapshotJson
);

public sealed record WorkoutGuidanceAiResult(
    string Json
);

public sealed record CoachClientBriefAiRequest(
    string Language,
    string SnapshotJson
);

public sealed record CoachClientBriefAiResult(
    string Json
);

public interface IUserAnalysisAi
{
    Task<UserAnalysisAiResult> GenerateAsync(UserAnalysisAiRequest request, CancellationToken cancellationToken);

    Task<StarterPlanAiResult> GenerateStarterPlanAsync(
        StarterPlanAiRequest request,
        CancellationToken cancellationToken);

    Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(
        PlanBalanceAiRequest request,
        CancellationToken cancellationToken);

    Task<WorkoutGuidanceAiResult> GenerateWorkoutGuidanceAsync(
        WorkoutGuidanceAiRequest request,
        CancellationToken cancellationToken);

    Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(
        CoachClientBriefAiRequest request,
        CancellationToken cancellationToken);
}
