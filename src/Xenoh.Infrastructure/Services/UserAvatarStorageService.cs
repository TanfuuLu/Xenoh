using Microsoft.AspNetCore.Hosting;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class UserAvatarStorageService(IWebHostEnvironment environment) : IUserAvatarStorageService
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    public async Task<string> SaveAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = ExtensionByContentType.TryGetValue(contentType, out var contentTypeExtension)
            ? contentTypeExtension
            : Path.GetExtension(fileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");

        var uploadRoot = Path.Combine(webRootPath, "uploads", "users-avatar");
        Directory.CreateDirectory(uploadRoot);

        var generatedFileName = $"{userId:N}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(uploadRoot, generatedFileName);

        await using var fileStream = File.Create(destinationPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/users-avatar/{generatedFileName}";
    }
}
