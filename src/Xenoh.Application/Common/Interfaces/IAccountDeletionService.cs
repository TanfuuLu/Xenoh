using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface IAccountDeletionService
{
    Task DeleteAccountAsync(
        Guid userId,
        AccountDeletionRequest? deletionRequest,
        string? accessToken,
        CancellationToken cancellationToken);
}
