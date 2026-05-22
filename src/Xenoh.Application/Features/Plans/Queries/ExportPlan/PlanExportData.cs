namespace Xenoh.Application.Features.Plans.Queries.ExportPlan;

public sealed record PlanExportData(
    string Name,
    DateOnly StartDate,
    IReadOnlyList<WeekExportData> Weeks
);

public sealed record WeekExportData(
    int WeekNumber,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<DayExportData> Days
);

public sealed record DayExportData(
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsCompleted,
    IReadOnlyList<ExerciseExportData> Exercises
);

public sealed record ExerciseExportData(
    string Name,
    int PlannedSets,
    int PlannedReps,
    decimal? PlannedWeight,
    int CompletedSetsCount,
    bool IsCompleted,
    string? Notes
);

public sealed record PlanCsvExportResult(byte[] Data, string FileName);
