using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class AccountDeletionObjectCleanerTests
{
    [Fact]
    public async Task DeleteAllAsync_WhenStorageFails_StopsWithoutReportingSuccessAndCanBeRetried()
    {
        var storage = new RetryableDocumentStorage(failFirstAttemptFor: "receipt-key");
        var keys = new[] { "profile-key", "receipt-key" };

        var firstAttempt = () => AccountDeletionObjectCleaner.DeleteAllAsync(
            storage,
            NullLogger.Instance,
            keys,
            CancellationToken.None);

        await firstAttempt.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Account deletion could not remove all stored documents*");

        await AccountDeletionObjectCleaner.DeleteAllAsync(
            storage,
            NullLogger.Instance,
            keys,
            CancellationToken.None);

        storage.SuccessfullyDeleted.Should().BeEquivalentTo("profile-key", "receipt-key");
    }

    private sealed class RetryableDocumentStorage(string failFirstAttemptFor) : IDocumentStorageService
    {
        private bool _failed;
        public HashSet<string> SuccessfullyDeleted { get; } = [];

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            if (!_failed && storageKey == failFirstAttemptFor)
            {
                _failed = true;
                throw new IOException("Temporary object-storage failure.");
            }

            SuccessfullyDeleted.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<string> SaveAsync(Guid ownerId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string> SaveChatAttachmentAsync(Guid senderId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string> GetPresignedDownloadUrlAsync(string storageKey, string downloadFileName, CancellationToken cancellationToken, bool inline = false) =>
            throw new NotSupportedException();
    }
}
