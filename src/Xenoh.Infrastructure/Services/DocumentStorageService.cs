using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class DocumentStorageService : IDocumentStorageService
{
    private readonly R2DocumentOptions _options;
    private readonly Lazy<IAmazonS3> _client;

    public DocumentStorageService(IOptions<R2DocumentOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public async Task<string> SaveAsync(
        Guid ownerId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);

        var extension = ValidateAndGetExtension(buffered.ToArray(), fileName, allowImages: false);

        buffered.Position = 0;

        var key = $"user-files/{ownerId:N}/{Guid.NewGuid():N}{extension}";
        return await PutAsync(key, extension, buffered, cancellationToken);
    }

    public async Task<string> SaveChatAttachmentAsync(
        Guid senderId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);

        var extension = ValidateAndGetExtension(buffered.ToArray(), fileName, allowImages: true);

        buffered.Position = 0;

        var key = $"chat-files/{senderId:N}/{Guid.NewGuid():N}{extension}";
        return await PutAsync(key, extension, buffered, cancellationToken);
    }

    private async Task<string> PutAsync(
        string key, string extension, Stream content, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            // Use a canonical content type for the extension; browser-supplied types
            // are unreliable (Windows often sends octet-stream for Office files).
            ContentType = CanonicalContentType(extension),
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _client.Value.PutObjectAsync(request, cancellationToken);

        return key;
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        string downloadFileName,
        CancellationToken cancellationToken,
        bool inline = false)
    {
        EnsureConfigured();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.DownloadUrlExpiryMinutes))
        };
        // Inline lets browsers render images in place; otherwise force a download
        // with the original file name.
        var disposition = inline ? "inline" : "attachment";
        request.ResponseHeaderOverrides.ContentDisposition =
            $"{disposition}; filename=\"{SanitizeFileName(downloadFileName)}\"";

        return await _client.Value.GetPreSignedURLAsync(request);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        await _client.Value.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken);
    }

    private IAmazonS3 CreateClient() =>
        new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                // Path-style keeps the bucket in the URL path — required for R2.
                ForcePathStyle = true
            });

    private void EnsureConfigured()
    {
        if (IsMissing(_options.AccountId) ||
            IsMissing(_options.BucketName) ||
            IsMissing(_options.AccessKeyId) ||
            IsMissing(_options.SecretAccessKey))
            throw new InvalidOperationException("R2 document storage is not configured.");
    }

    private static bool IsMissing(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileName(string fileName)
    {
        // Strip characters that could break the Content-Disposition header.
        var cleaned = fileName.Replace("\"", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();
        return string.IsNullOrEmpty(cleaned) ? "document" : cleaned;
    }

    /// <summary>
    /// Validates the file by its extension plus the container's magic bytes, and returns
    /// the canonical extension. We key off the extension (not the browser-supplied content
    /// type, which is unreliable for Office files) and confirm the byte signature matches
    /// the expected container — PDF (%PDF), OOXML docx/xlsx (ZIP), legacy doc/xls (OLE2).
    /// Throws with a specific reason so the client can show actionable feedback.
    /// </summary>
    private static string ValidateAndGetExtension(byte[] bytes, string fileName, bool allowImages)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        var (signatureOk, expectedKind) = ext switch
        {
            ".pdf" => (IsPdf(bytes), "PDF"),
            ".docx" => (IsZip(bytes), "Word (.docx)"),
            ".xlsx" => (IsZip(bytes), "Excel (.xlsx)"),
            ".doc" => (IsOle2(bytes), "Word (.doc)"),
            ".xls" => (IsOle2(bytes), "Excel (.xls)"),
            ".png" when allowImages => (IsPng(bytes), "PNG image"),
            ".jpg" or ".jpeg" when allowImages => (IsJpeg(bytes), "JPEG image"),
            ".gif" when allowImages => (IsGif(bytes), "GIF image"),
            ".webp" when allowImages => (IsWebp(bytes), "WebP image"),
            _ => (false, null as string)
        };

        if (expectedKind is null)
        {
            var allowed = allowImages
                ? "Only images, PDF, Word, and Excel files are allowed."
                : "Only PDF, Word, and Excel documents are allowed.";
            throw new InvalidOperationException($"Unsupported file type '{ext}'. {allowed}");
        }

        if (!signatureOk)
            throw new InvalidOperationException(
                $"The file does not look like a valid {expectedKind} (got {bytes.Length} bytes).");

        return ext;
    }

    private static string CanonicalContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;

    private static bool IsZip(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;

    private static bool IsOle2(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0 &&
        bytes[4] == 0xA1 && bytes[5] == 0xB1 && bytes[6] == 0x1A && bytes[7] == 0xE1;

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
        bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length >= 3 &&
        bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    private static bool IsGif(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38;

    // RIFF....WEBP — "RIFF" at 0, "WEBP" at offset 8.
    private static bool IsWebp(byte[] bytes) =>
        bytes.Length >= 12 &&
        bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
        bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
}
