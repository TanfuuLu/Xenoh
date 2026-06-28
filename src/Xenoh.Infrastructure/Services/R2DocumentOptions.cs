namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Settings for the private R2 bucket that holds user documents. Unlike avatars/share
/// images, documents are never public, so there is no PublicBaseUrl — reads go through
/// short-lived presigned URLs.
/// </summary>
public sealed class R2DocumentOptions
{
    public const string SectionName = "R2Documents";

    public string AccountId { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string AccessKeyId { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;

    /// <summary>How long a generated download URL stays valid.</summary>
    public int DownloadUrlExpiryMinutes { get; init; } = 5;
}
