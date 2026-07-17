namespace Xenoh.Application.Features.TrainingDayShares;

public sealed record TrainingDayShareResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    Guid SourceDailyWorkoutId,
    DateOnly WorkoutDate,
    string DayOfWeek,
    string DayStatus,
    int ExerciseCount,
    int CompletedSets,
    decimal TotalVolume,
    int TotalDurationSeconds,
    decimal? AverageRpe,
    bool HasPersonalRecord,
    string? Caption,
    int LoveCount,
    bool LovedByCurrentUser,
    bool IsReusable,
    DateTime CreatedAt,
    IReadOnlyList<TrainingDayShareExerciseResponse> Exercises);

public sealed record TrainingDayShareExerciseResponse(
    Guid Id,
    string Name,
    string PrimaryMuscleGroup,
    string ExerciseKind,
    Guid ExerciseTemplateId,
    int SortOrder,
    bool IsSkipped,
    bool IsPersonalRecord,
    int? DurationSeconds,
    string? Notes,
    IReadOnlyList<TrainingDayShareSetResponse> Sets);

public sealed record TrainingDayShareSetResponse(
    Guid Id,
    int SetNumber,
    int PlannedReps,
    decimal? PlannedWeight,
    int? ActualReps,
    decimal? ActualWeight,
    decimal? Rpe,
    bool IsCompleted);
