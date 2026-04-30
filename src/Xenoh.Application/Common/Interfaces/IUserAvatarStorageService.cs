namespace Xenoh.Application.Common.Interfaces;

public interface IUserAvatarStorageService
{
    Task<string> SaveAsync(
        Guid userId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);
}
