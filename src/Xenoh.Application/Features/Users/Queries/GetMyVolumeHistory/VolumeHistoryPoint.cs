namespace Xenoh.Application.Features.Users.Queries.GetMyVolumeHistory;

/// <summary>Completed ("done") training volume for a single calendar month.</summary>
public sealed record VolumeHistoryPoint(int Year, int Month, decimal VolumeKg);
