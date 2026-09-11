namespace Xenoh.Domain.Rules;

public static class ProgressLibraryRules
{
    public const int MaximumPhotosPerCheckIn = 3;
    public const long MaximumPhotoSizeBytes = 5L * 1024 * 1024;

    public static bool HasValidPhotoCount(int count) => count is >= 1 and <= MaximumPhotosPerCheckIn;

    public static bool HasValidCheckInDate(DateOnly date) => date <= DateOnly.FromDateTime(DateTime.UtcNow);
}
