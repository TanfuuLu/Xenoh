namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Cloudflare R2 settings for the public PR share-card bucket
/// (e.g. xenoh_bucket, served from a public assets domain under share-images/).
/// Kept separate from <see cref="R2AvatarOptions"/> so it can use its own
/// bucket and scoped credentials.
/// </summary>
public sealed class R2ShareOptions
{
    public const string SectionName = "R2Share";

    public string AccountId { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string AccessKeyId { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;
    public string PublicBaseUrl { get; init; } = string.Empty;
}
