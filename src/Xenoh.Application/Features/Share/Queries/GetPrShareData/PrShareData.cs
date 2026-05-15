namespace Xenoh.Application.Features.Share.Queries.GetPrShareData;

public sealed record PrShareData(
    string UserName,
    string ExerciseName,
    decimal WeightKg,
    int Reps,
    DateTime AchievedAt,
    int Level,
    bool IsCompetitionLift
);
