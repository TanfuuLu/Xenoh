using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Supplements;

public sealed record SupplementDoseSlotRequest(
    decimal Amount,
    string Unit,
    TimeOnly Time,
    IReadOnlyList<DayOfWeek> DaysOfWeek);

public sealed record CreateSupplementRegimenRequest(
    string Name,
    string? Brand,
    string? Form,
    string? Instructions,
    string? Notes,
    DateOnly? StartDate,
    IReadOnlyList<SupplementDoseSlotRequest> DoseSlots);

public sealed record UpdateSupplementRegimenRequest(
    string Name,
    string? Brand,
    string? Form,
    string? Instructions,
    string? Notes,
    DateOnly EffectiveFrom,
    IReadOnlyList<SupplementDoseSlotRequest> DoseSlots);

public sealed record RecordSupplementDoseRequest(
    SupplementIntakeStatus Status,
    DateTime? RecordedAt,
    string? Note);

public sealed record SupplementCreatorResponse(
    Guid Id,
    string FullName);

public sealed record SupplementDoseSlotResponse(
    Guid Id,
    decimal Amount,
    string Unit,
    TimeOnly Time,
    IReadOnlyList<DayOfWeek> DaysOfWeek);

public sealed record SupplementRegimenResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Brand,
    string? Form,
    string? Instructions,
    string? Notes,
    bool IsArchived,
    DateTime? ArchivedAt,
    SupplementCreatorResponse? CreatedBy,
    Guid? ScheduleVersionId,
    DateOnly? ScheduleEffectiveFrom,
    DateOnly? ScheduleEffectiveTo,
    IReadOnlyList<SupplementDoseSlotResponse> DoseSlots,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public enum SupplementDoseStatus
{
    Pending,
    Taken,
    Skipped,
    Missed
}

public sealed record SupplementDailyDoseResponse(
    Guid DoseSlotId,
    Guid RegimenId,
    string RegimenName,
    string? Brand,
    string? Form,
    decimal Amount,
    string Unit,
    TimeOnly Time,
    SupplementDoseStatus Status,
    DateTime? RecordedAt,
    string? Note);

public sealed record SupplementAdherenceTotals(
    int Planned,
    int Taken,
    int Skipped,
    int Missed,
    int Pending,
    decimal? AdherencePercentage);

public sealed record SupplementDailyResponse(
    Guid UserId,
    DateOnly Date,
    IReadOnlyList<SupplementDailyDoseResponse> Doses,
    SupplementAdherenceTotals Totals);

public sealed record SupplementHistoryDayResponse(
    DateOnly Date,
    SupplementAdherenceTotals Totals);

public sealed record SupplementHistoryResponse(
    Guid UserId,
    DateOnly From,
    DateOnly To,
    SupplementAdherenceTotals Totals,
    IReadOnlyList<SupplementHistoryDayResponse> Days);
