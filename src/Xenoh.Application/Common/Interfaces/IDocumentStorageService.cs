namespace Xenoh.Application.Common.Interfaces;

public interface IDocumentStorageService
{
    /// <summary>
    /// Validates the document (PDF / Word, by magic bytes) and stores it in R2.
    /// Returns the R2 object key. Throws <see cref="InvalidOperationException"/> for
    /// unsupported content.
    /// </summary>
    Task<string> SaveAsync(
        Guid ownerId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a short-lived presigned GET URL that downloads the object as an
    /// attachment with the given file name. The bucket is private, so this is the
    /// only way to read a stored file.
    /// </summary>
    Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        string downloadFileName,
        CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
