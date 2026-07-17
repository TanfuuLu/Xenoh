using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeEmailService : IEmailService
{
    public List<(string ToEmail, string FullName, string Code)> SentCodes { get; } = [];

    public Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken)
    {
        SentCodes.Add((toEmail, fullName, code));
        return Task.CompletedTask;
    }

    public Task SendAccountDeletionVerificationAsync(string toEmail, string fullName, string verificationUrl, CancellationToken cancellationToken) => Task.CompletedTask;
}
