using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class UserAvatarStorageService : IUserAvatarStorageService
{
    private readonly Func<PutObjectRequest, CancellationToken, Task<PutObjectResponse>> _putObjectAsync;
    private readonly IOptions<R2AvatarOptions> _options;

    public UserAvatarStorageService(IAmazonS3 s3, IOptions<R2AvatarOptions> options)
    {
        _putObjectAsync = (request, cancellationToken) => s3.PutObjectAsync(request, cancellationToken);
        _options = options;
    }

    public UserAvatarStorageService(
        Func<PutObjectRequest, CancellationToken, Task<PutObjectResponse>> putObjectAsync,
        IOptions<R2AvatarOptions> options)
    {
        _putObjectAsync = putObjectAsync;
        _options = options;
    }

    public async Task<string> SaveAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var avatarOptions = _options.Value;
        EnsureConfigured(avatarOptions);

        await using var bufferedContent = new MemoryStream();
        await content.CopyToAsync(bufferedContent, cancellationToken);

        if (!TryGetValidatedImageExtension(bufferedContent.ToArray(), contentType, out var extension))
            throw new InvalidOperationException("Avatar image content is invalid.");

        bufferedContent.Position = 0;

        var key = $"users-avatar/{userId:N}-{Guid.NewGuid():N}{extension}";
        var request = new PutObjectRequest
        {
            BucketName = avatarOptions.BucketName,
            Key = key,
            InputStream = bufferedContent,
            ContentType = contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };
        request.Headers.CacheControl = "public, max-age=31536000, immutable";

        await _putObjectAsync(request, cancellationToken);

        return $"{avatarOptions.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    private static void EnsureConfigured(R2AvatarOptions options)
    {
        if (IsMissing(options.AccountId) ||
            IsMissing(options.BucketName) ||
            IsMissing(options.AccessKeyId) ||
            IsMissing(options.SecretAccessKey) ||
            IsMissing(options.PublicBaseUrl))
            throw new InvalidOperationException("R2 avatar storage is not configured.");
    }

    private static bool IsMissing(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("_SECRET", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetValidatedImageExtension(byte[] bytes, string contentType, out string extension)
    {
        extension = string.Empty;

        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) &&
            bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF)
        {
            extension = ".jpg";
            return true;
        }

        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) &&
            bytes.Length >= 8 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47 &&
            bytes[4] == 0x0D &&
            bytes[5] == 0x0A &&
            bytes[6] == 0x1A &&
            bytes[7] == 0x0A)
        {
            extension = ".png";
            return true;
        }

        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase) &&
            bytes.Length >= 12 &&
            bytes[0] == 0x52 &&
            bytes[1] == 0x49 &&
            bytes[2] == 0x46 &&
            bytes[3] == 0x46 &&
            bytes[8] == 0x57 &&
            bytes[9] == 0x45 &&
            bytes[10] == 0x42 &&
            bytes[11] == 0x50)
        {
            extension = ".webp";
            return true;
        }

        if (contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase) &&
            bytes.Length >= 6 &&
            bytes[0] == 0x47 &&
            bytes[1] == 0x49 &&
            bytes[2] == 0x46 &&
            bytes[3] == 0x38 &&
            (bytes[4] == 0x37 || bytes[4] == 0x39) &&
            bytes[5] == 0x61)
        {
            extension = ".gif";
            return true;
        }

        return false;
    }
}
