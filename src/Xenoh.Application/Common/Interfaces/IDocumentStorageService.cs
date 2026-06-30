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
    /// Validates a chat attachment (images + PDF / Word / Excel, by magic bytes) and
    /// stores it in R2 under a chat-files prefix. Returns the R2 object key. Throws
    /// <see cref="InvalidOperationException"/> for unsupported content. Unlike
    /// <see cref="SaveAsync"/> (documents only), this accepts image types.
    /// </summary>
    Task<string> SaveChatAttachmentAsync(
        Guid senderId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a short-lived presigned GET URL. When <paramref name="inline"/> is
    /// false (default) the object downloads as an attachment with the given file name;
    /// when true the object is served inline so browsers can render it (used for image
    /// previews). The bucket is private, so this is the only way to read a stored file.
    /// </summary>
    Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        string downloadFileName,
        CancellationToken cancellationToken,
        bool inline = false);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
