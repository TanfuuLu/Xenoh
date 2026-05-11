using Mediator;

namespace Xenoh.Application.Features.Dashboard.Queries.GetPersonalDashboard;

public sealed record GetPersonalDashboardQuery : IRequest<PersonalDashboardResponse>;

public sealed record PersonalDashboardResponse(
    PersonalDashboardProfileResponse Profile,
    PersonalDashboardPlanResponse? ActivePlan,
    PersonalDashboardTodayWorkoutResponse? TodayWorkout,
    PersonalDashboardNutritionResponse NutritionToday,
    List<PersonalDashboardActionResponse> NextActions,
    PersonalDashboardProInsightsResponse ProInsights
);

public sealed record PersonalDashboardProfileResponse(
    string FirstName,
    string? AvatarUrl,
    int CurrentStreak,
    int Level,
    long TotalXp,
    long XpToNextLevel,
    string Title,
    decimal? LatestBodyweight,
    decimal? Bmi,
    string? BmiCategory,
    decimal? DotsScore
);

public sealed record PersonalDashboardPlanResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    int CompletedDays,
    int ProgressPercent,
    PersonalDashboardWeekResponse? CurrentWeek
);

public sealed record PersonalDashboardWeekResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    int CompletedDays,
    int ProgressPercent
);

public sealed record PersonalDashboardTodayWorkoutResponse(
    Guid Id,
    Guid WeeklyWorkoutId,
    string DayOfWeek,
    DateOnly Date,
    string Status,
    bool IsCompleted,
    int TotalExercises,
    int CompletedExercises,
    int TotalSets,
    int CompletedSets,
    decimal PlannedVolume,
    List<string> MuscleGroups,
    string Route
);

public sealed record PersonalDashboardNutritionResponse(
    int? CalorieTarget,
    decimal? ProteinTargetG,
    decimal? CarbsTargetG,
    decimal? FatTargetG,
    int LoggedCalories,
    decimal LoggedProteinG,
    decimal LoggedCarbsG,
    decimal LoggedFatG,
    int? RemainingCalories,
    decimal? RemainingProteinG,
    decimal? RemainingCarbsG,
    decimal? RemainingFatG,
    List<string> MissingProfileFields
);

public sealed record PersonalDashboardActionResponse(
    string Type,
    string Label,
    string Description,
    string Route,
    int Priority
);

public sealed record PersonalDashboardProInsightsResponse(
    bool IsUnlocked,
    string? CtaLabel,
    string? CtaRoute,
    List<PersonalDashboardInsightResponse> Items
);

public sealed record PersonalDashboardInsightResponse(
    string Type,
    string Severity,
    string Title,
    string Message
);
