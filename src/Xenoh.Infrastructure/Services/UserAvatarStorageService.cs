using Microsoft.AspNetCore.Hosting;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class UserAvatarStorageService(IWebHostEnvironment environment) : IUserAvatarStorageService
{
    public async Task<string> SaveAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        await using var bufferedContent = new MemoryStream();
        await content.CopyToAsync(bufferedContent, cancellationToken);

        if (!TryGetValidatedImageExtension(bufferedContent.ToArray(), contentType, out var extension))
            throw new InvalidOperationException("Avatar image content is invalid.");

        bufferedContent.Position = 0;

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");

        var uploadRoot = Path.Combine(webRootPath, "uploads", "users-avatar");
        Directory.CreateDirectory(uploadRoot);

        var generatedFileName = $"{userId:N}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(uploadRoot, generatedFileName);

        await using var fileStream = File.Create(destinationPath);
        await bufferedContent.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/users-avatar/{generatedFileName}";
    }

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
