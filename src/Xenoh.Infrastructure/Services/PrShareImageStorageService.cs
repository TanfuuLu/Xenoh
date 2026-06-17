using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.Infrastructure.Services;

public sealed class PrShareImageStorageService : IPrShareImageStorage
{
    // Bump when the card rendering changes so cached objects are regenerated.
    private const string RenderVersion = "v2";

    private readonly R2ShareOptions _options;
    private readonly Lazy<IAmazonS3> _client;

    public PrShareImageStorageService(IOptions<R2ShareOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public bool IsConfigured =>
        !IsMissing(_options.AccountId) &&
        !IsMissing(_options.BucketName) &&
        !IsMissing(_options.AccessKeyId) &&
        !IsMissing(_options.SecretAccessKey) &&
        !IsMissing(_options.PublicBaseUrl);

    public async Task<string> EnsureAsync(
        Guid userId,
        Guid exerciseTemplateId,
        PrShareData data,
        Func<CancellationToken, Task<byte[]>> render,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        var key = BuildKey(userId, exerciseTemplateId, data);

        if (!await ExistsAsync(key, ct))
        {
            var bytes = await render(ct);
            await UploadAsync(key, bytes, ct);
        }

        return PublicUrl(key);
    }

    public async Task<byte[]?> TryGetAsync(
        Guid userId,
        Guid exerciseTemplateId,
        PrShareData data,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        var key = BuildKey(userId, exerciseTemplateId, data);
        try
        {
            using var response = await _client.Value.GetObjectAsync(_options.BucketName, key, ct);
            await using var stream = response.ResponseStream;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            await _client.Value.GetObjectMetadataAsync(_options.BucketName, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task UploadAsync(string key, byte[] bytes, CancellationToken ct)
    {
        using var content = new MemoryStream(bytes, writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = "image/png",
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };
        // Objects are immutable: the key embeds a hash of the PR contents.
        request.Headers.CacheControl = "public, max-age=31536000, immutable";

        await _client.Value.PutObjectAsync(request, ct);
    }

    private string PublicUrl(string key) => $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

    private IAmazonS3 CreateClient() =>
        new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                // Path-style keeps the bucket in the URL path instead of the hostname,
                // which is required for R2 and avoids DNS issues with bucket names.
                ForcePathStyle = true
            });

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("R2 share image storage is not configured.");
    }

    private static string BuildKey(Guid userId, Guid exerciseTemplateId, PrShareData data)
    {
        // Versioned key so a new/improved PR (or a card redesign) produces a new
        // immutable object instead of serving a stale cached image.
        var token = $"{RenderVersion}|{data.WeightKg:0.###}|{data.Reps}|{data.AchievedAt.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..12].ToLowerInvariant();
        return $"share-images/{userId:N}-{exerciseTemplateId:N}-{hash}.png";
    }

    private static bool IsMissing(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}
