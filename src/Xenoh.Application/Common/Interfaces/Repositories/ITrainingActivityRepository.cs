namespace Xenoh.Application.Common.Interfaces.Repositories;

public sealed record TrainingActivitySnapshot(
    long TotalDurationSeconds,
    decimal TotalWeightTrainedKg,
    List<DateOnly> TrainedDates);

/// <summary>Completed ("done") training volume aggregated for a single calendar month.</summary>
public sealed record MonthlyVolumeBucket(int Year, int Month, decimal VolumeKg);

public interface ITrainingActivityRepository
{
    Task<TrainingActivitySnapshot> GetActivityAsync(
        Guid userId,
        DateOnly accountCreatedDate,
        DateOnly today,
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Sum of completed-set volume bucketed by calendar month, for workout days in
    /// <paramref name="fromDate"/>..<paramref name="today"/>. Only months with volume are returned.
    /// </summary>
    Task<List<MonthlyVolumeBucket>> GetMonthlyVolumeAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly today,
        CancellationToken ct = default);
}
