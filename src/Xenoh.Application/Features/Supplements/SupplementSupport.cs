using Mapster;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Supplements;

internal static class SupplementAccess
{
    public static async Task<Guid> ResolveTargetUserAsync(
        Guid callerId,
        Guid? requestedUserId,
        ICoachClientRepository coachClientRepository,
        CancellationToken cancellationToken)
    {
        var targetUserId = requestedUserId ?? callerId;
        if (targetUserId == callerId)
            return targetUserId;

        if (await coachClientRepository.FindActiveByCoachAndClientAsync(
                callerId,
                targetUserId,
                cancellationToken) is null)
        {
            throw new UnauthorizedAccessException(
                "You do not have access to this user's supplement data.");
        }

        return targetUserId;
    }
}

internal static class SupplementRules
{
    private static readonly SupplementWeekdays AllWeekdays = SupplementWeekdays.EveryDay;

    public static void ValidateRegimen(
        string name,
        string? brand,
        string? form,
        string? instructions,
        string? notes,
        IReadOnlyList<SupplementDoseSlotRequest> doseSlots)
    {
        ValidateText(name, nameof(name), 100, required: true);
        ValidateText(brand, nameof(brand), 100);
        ValidateText(form, nameof(form), 50);
        ValidateText(instructions, nameof(instructions), 500);
        ValidateText(notes, nameof(notes), 500);

        if (doseSlots.Count is < 1 or > 20)
            throw new InvalidOperationException("A regimen must contain between 1 and 20 dose slots.");

        var normalized = doseSlots.Select((slot, index) =>
        {
            if (slot.Amount <= 0 || slot.Amount > 1_000_000)
                throw new InvalidOperationException($"Dose slot {index + 1} has an invalid amount.");

            ValidateText(slot.Unit, $"doseSlots[{index}].unit", 30, required: true);
            if (slot.DaysOfWeek.Count == 0)
                throw new InvalidOperationException($"Dose slot {index + 1} must have at least one weekday.");

            var weekdays = ToMask(slot.DaysOfWeek);
            if (weekdays == SupplementWeekdays.None || (weekdays & ~AllWeekdays) != 0)
                throw new InvalidOperationException($"Dose slot {index + 1} contains an invalid weekday.");

            return (slot.Time, Weekdays: weekdays);
        }).ToList();

        for (var i = 0; i < normalized.Count; i++)
        {
            for (var j = i + 1; j < normalized.Count; j++)
            {
                if (normalized[i].Time == normalized[j].Time &&
                    (normalized[i].Weekdays & normalized[j].Weekdays) != 0)
                {
                    throw new InvalidOperationException(
                        "Dose slots at the same time cannot contain overlapping weekdays.");
                }
            }
        }
    }

    public static SupplementWeekdays ToMask(IEnumerable<DayOfWeek> days)
    {
        var mask = SupplementWeekdays.None;
        foreach (var day in days.Distinct())
        {
            mask |= day switch
            {
                DayOfWeek.Sunday => SupplementWeekdays.Sunday,
                DayOfWeek.Monday => SupplementWeekdays.Monday,
                DayOfWeek.Tuesday => SupplementWeekdays.Tuesday,
                DayOfWeek.Wednesday => SupplementWeekdays.Wednesday,
                DayOfWeek.Thursday => SupplementWeekdays.Thursday,
                DayOfWeek.Friday => SupplementWeekdays.Friday,
                DayOfWeek.Saturday => SupplementWeekdays.Saturday,
                _ => SupplementWeekdays.None
            };
        }

        return mask;
    }

    public static IReadOnlyList<DayOfWeek> FromMask(SupplementWeekdays mask) =>
        Enum.GetValues<DayOfWeek>()
            .Where(day => (mask & ToMask([day])) != 0)
            .ToArray();

    public static SupplementScheduleVersion BuildVersion(
        DateOnly effectiveFrom,
        Guid creatorId,
        IReadOnlyList<SupplementDoseSlotRequest> doseSlots) =>
        new()
        {
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = creatorId,
            DoseSlots = doseSlots.Select(slot => new SupplementDoseSlot
            {
                Amount = slot.Amount,
                Unit = slot.Unit.Trim(),
                Time = slot.Time,
                Weekdays = ToMask(slot.DaysOfWeek)
            }).ToList()
        };

    public static void ApplyMetadata(
        SupplementRegimen regimen,
        string name,
        string? brand,
        string? form,
        string? instructions,
        string? notes)
    {
        regimen.Name = name.Trim();
        regimen.Brand = TrimOrNull(brand);
        regimen.Form = TrimOrNull(form);
        regimen.Instructions = TrimOrNull(instructions);
        regimen.Notes = TrimOrNull(notes);
        regimen.UpdatedAt = DateTime.UtcNow;
    }

    public static void ValidateHistoryRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new InvalidOperationException("'from' must be on or before 'to'.");
        if (to.DayNumber - from.DayNumber > 89)
            throw new InvalidOperationException("Supplement history is limited to 90 days.");
    }

    public static SupplementAdherenceTotals CalculateTotals(
        IEnumerable<SupplementDoseStatus> statuses)
    {
        var values = statuses.ToList();
        var taken = values.Count(x => x == SupplementDoseStatus.Taken);
        var skipped = values.Count(x => x == SupplementDoseStatus.Skipped);
        var missed = values.Count(x => x == SupplementDoseStatus.Missed);
        var pending = values.Count(x => x == SupplementDoseStatus.Pending);
        var completedDenominator = taken + skipped + missed;
        decimal? adherence = completedDenominator == 0
            ? null
            : Math.Round(taken * 100m / completedDenominator, 1);

        return new SupplementAdherenceTotals(
            values.Count,
            taken,
            skipped,
            missed,
            pending,
            adherence);
    }

    private static void ValidateText(string? value, string field, int maxLength, bool required = false)
    {
        if (required && string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{field} is required.");
        if (value?.Trim().Length > maxLength)
            throw new InvalidOperationException($"{field} cannot exceed {maxLength} characters.");
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class SupplementMapper
{
    private static readonly TypeAdapterConfig Config = BuildConfig();

    public static SupplementRegimenResponse ToResponse(SupplementRegimen regimen, DateOnly today)
    {
        var version = regimen.ScheduleVersions
            .Where(x => x.EffectiveFrom > today)
            .OrderBy(x => x.EffectiveFrom)
            .FirstOrDefault()
            ?? regimen.ScheduleVersions
                .Where(x => x.IsEffectiveOn(today))
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefault()
            ?? regimen.ScheduleVersions.OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();

        return new SupplementRegimenResponse(
            regimen.Id,
            regimen.UserId,
            regimen.Name,
            regimen.Brand,
            regimen.Form,
            regimen.Instructions,
            regimen.Notes,
            regimen.IsArchived,
            regimen.ArchivedAt,
            regimen.CreatedByUser is null
                ? null
                : new SupplementCreatorResponse(
                    regimen.CreatedByUser.Id,
                    $"{regimen.CreatedByUser.FirstName} {regimen.CreatedByUser.LastName}".Trim()),
            version?.Id,
            version?.EffectiveFrom,
            version?.EffectiveTo,
            version?.DoseSlots
                .OrderBy(x => x.Time)
                .Select(x => x.Adapt<SupplementDoseSlotResponse>(Config))
                .ToList()
                ?? [],
            regimen.CreatedAt,
            regimen.UpdatedAt);
    }

    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<SupplementDoseSlot, SupplementDoseSlotResponse>()
            .Map(
                destination => destination.DaysOfWeek,
                source => SupplementRules.FromMask(source.Weekdays));
        return config;
    }
}

internal static class SupplementOccurrenceFactory
{
    public static SupplementDailyDoseResponse ToDailyDose(
        SupplementDoseSlot slot,
        SupplementIntakeLog? intake,
        DateOnly scheduledDate,
        DateOnly today) =>
        new(
            slot.Id,
            slot.ScheduleVersion.Regimen.Id,
            slot.ScheduleVersion.Regimen.Name,
            slot.ScheduleVersion.Regimen.Brand,
            slot.ScheduleVersion.Regimen.Form,
            slot.Amount,
            slot.Unit,
            slot.Time,
            ResolveStatus(intake, scheduledDate, today),
            intake?.RecordedAt,
            intake?.Note);

    public static SupplementDoseStatus ResolveStatus(
        SupplementIntakeLog? intake,
        DateOnly scheduledDate,
        DateOnly today)
    {
        if (intake?.Status == SupplementIntakeStatus.Taken)
            return SupplementDoseStatus.Taken;
        if (intake?.Status == SupplementIntakeStatus.Skipped)
            return SupplementDoseStatus.Skipped;
        return scheduledDate < today
            ? SupplementDoseStatus.Missed
            : SupplementDoseStatus.Pending;
    }
}
