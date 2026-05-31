namespace Xenoh.Application.Features.Users.Queries.GetMyTrainingActivity;

public sealed record TrainingActivityResponse(
    long TotalDurationSeconds,
    decimal TotalWeightTrainedKg,
    DateTime AccountCreatedAt,
    int Year,
    int Month,
    IReadOnlyList<DateOnly> TrainedDates);
