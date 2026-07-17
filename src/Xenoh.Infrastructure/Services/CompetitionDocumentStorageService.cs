using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class CompetitionDocumentStorageService(IDocumentStorageService documents) : ICompetitionDocumentStorageService
{
    public Task<string> SaveReceiptAsync(Guid userId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        documents.SaveChatAttachmentAsync(userId, fileName, contentType, content, cancellationToken);

    public Task<string> GetReceiptUrlAsync(string storageKey, string fileName, CancellationToken cancellationToken) =>
        documents.GetPresignedDownloadUrlAsync(storageKey, fileName, cancellationToken, inline: true);
}
