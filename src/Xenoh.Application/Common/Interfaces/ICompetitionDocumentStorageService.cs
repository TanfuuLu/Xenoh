namespace Xenoh.Application.Common.Interfaces;

public interface ICompetitionDocumentStorageService
{
    Task<string> SaveReceiptAsync(Guid userId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<string> GetReceiptUrlAsync(string storageKey, string fileName, CancellationToken cancellationToken);
}
