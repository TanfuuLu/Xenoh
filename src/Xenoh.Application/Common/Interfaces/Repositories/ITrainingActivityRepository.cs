namespace Xenoh.Application.Common.Interfaces.Repositories;

public sealed record TrainingActivitySnapshot(
    long TotalDurationSeconds,
    decimal TotalWeightTrainedKg,
    List<DateOnly> TrainedDates);

public interface ITrainingActivityRepository
{
    Task<TrainingActivitySnapshot> GetActivityAsync(
        Guid userId,
        DateOnly accountCreatedDate,
        DateOnly today,
        int year,
        int month,
        CancellationToken ct = default);
}
