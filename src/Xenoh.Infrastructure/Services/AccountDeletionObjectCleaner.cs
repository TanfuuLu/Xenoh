using Microsoft.Extensions.Logging;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public static class AccountDeletionObjectCleaner
{
    public static async Task DeleteAllAsync(
        IDocumentStorageService documentStorage,
        ILogger logger,
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken)
    {
        foreach (var key in storageKeys
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.Ordinal))
        {
            try
            {
                await documentStorage.DeleteAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account deletion stopped because a stored object could not be deleted.");
                throw new InvalidOperationException(
                    "Account deletion could not remove all stored documents. The request can be retried safely.",
                    ex);
            }
        }
    }
}
