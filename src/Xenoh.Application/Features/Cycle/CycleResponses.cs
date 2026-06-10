namespace Xenoh.Application.Features.Cycle;

public sealed record CycleDailyLogResponse(
    DateOnly Date,
    string? Flow,
    IReadOnlyList<string> Symptoms,
    string? Mood,
    int? EnergyLevel,
    string? Notes);

public sealed record PredictedPeriodResponse(DateOnly Start, DateOnly End);

public sealed record FertileWindowResponse(DateOnly Start, DateOnly End);

public sealed record CycleOverviewResponse(
    string CurrentPhase,
    int? CycleDay,
    int? DaysUntilNextPeriod,
    int? DaysLate,
    DateOnly? LastPeriodStart,
    DateOnly? NextPeriodStart,
    DateOnly? CurrentPeriodPredictedEnd,
    IReadOnlyList<PredictedPeriodResponse> PredictedPeriods,
    IReadOnlyList<DateOnly> OvulationDates,
    IReadOnlyList<FertileWindowResponse> FertileWindows,
    int EffectiveCycleLengthDays,
    int EffectivePeriodLengthDays,
    int? AvgCycleLengthDays,
    int? AvgPeriodLengthDays,
    bool IsRegular,
    int? CycleVariabilityDays,
    string Confidence,
    bool NeedsData);

public sealed record CycleSettingsResponse(
    int? AverageCycleLengthOverride,
    int? AveragePeriodLengthOverride,
    bool ShareWithCoach);

/// <summary>A single plan/calendar day annotated with its cycle marker.</summary>
public sealed record CycleDayMarkerResponse(DateOnly Date, string Marker);

/// <summary>
/// Menstrual / pre-menstrual day markers across a requested date window, used to overlay
/// cycle awareness onto training plans. Only non-empty markers are returned in <see cref="Days"/>.
/// </summary>
public sealed record CycleDayMarkersResponse(
    DateOnly From,
    DateOnly To,
    int PreMenstrualWindowDays,
    bool NeedsData,
    IReadOnlyList<CycleDayMarkerResponse> Days);

/// <summary>
/// Coach-facing summary: phase, predictions, and the client's most frequent recent symptoms.
/// Deliberately excludes per-day flow, mood, energy, and free-text notes (kept private).
/// </summary>
public sealed record ClientCycleOverviewResponse(
    string CurrentPhase,
    int? CycleDay,
    DateOnly? NextPeriodStart,
    int? DaysUntilNextPeriod,
    int? DaysLate,
    int EffectiveCycleLengthDays,
    int EffectivePeriodLengthDays,
    int? AvgCycleLengthDays,
    int? AvgPeriodLengthDays,
    bool IsRegular,
    int? CycleVariabilityDays,
    DateOnly? LastPeriodStart,
    DateOnly? NextOvulationDate,
    DateOnly? FertileWindowStart,
    DateOnly? FertileWindowEnd,
    IReadOnlyList<string> FrequentSymptoms,
    string Confidence,
    bool NeedsData);

public sealed record CycleInsightPhaseRecommendation(
    string Phase,
    string Training,
    string Nutrition);

public sealed record CycleInsightContent(
    string Summary,
    IReadOnlyList<string> CyclePatterns,
    IReadOnlyList<string> SymptomPatterns,
    IReadOnlyList<string> TrainingCorrelations,
    IReadOnlyList<CycleInsightPhaseRecommendation> PhaseRecommendations,
    IReadOnlyList<string> Cautions,
    string Disclaimer);

public sealed record CycleInsightResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    CycleInsightContent Content);
