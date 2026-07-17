using FluentAssertions;
using Xunit;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Auth.Commands.AccountDeletion;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class DeleteMyAccountHandlerTests : IdentityHandlerTestBase
{
    private readonly FakeAccountDeletionService _deletionService = new();

    private DeleteMyAccountHandler CreateHandler() =>
        new(CurrentUser(), CreateUserManager(), _deletionService);

    [Fact]
    public async Task Handle_WhenPasswordIsCorrect_DeletesAccount()
    {
        await SeedUserAsync("user@test.com", "CorrectPass1");

        await CreateHandler().Handle(new DeleteMyAccountCommand
        {
            Password = "CorrectPass1",
            AccessToken = "current-access-token"
        }, CancellationToken.None);

        _deletionService.UserId.Should().Be(UserId);
        _deletionService.AccessToken.Should().Be("current-access-token");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_DoesNotDeleteAccount()
    {
        await SeedUserAsync("user@test.com", "CorrectPass1");

        var act = () => CreateHandler().Handle(new DeleteMyAccountCommand
        {
            Password = "WrongPass1"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Password is incorrect.");
        _deletionService.UserId.Should().BeNull();
    }

    private sealed class FakeAccountDeletionService : IAccountDeletionService
    {
        public Guid? UserId { get; private set; }
        public string? AccessToken { get; private set; }

        public Task DeleteAccountAsync(
            Guid userId,
            AccountDeletionRequest? deletionRequest,
            string? accessToken,
            CancellationToken cancellationToken)
        {
            UserId = userId;
            AccessToken = accessToken;
            return Task.CompletedTask;
        }
    }
}
