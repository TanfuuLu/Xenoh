namespace Xenoh.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken);
    Task SendAccountDeletionVerificationAsync(string toEmail, string fullName, string verificationUrl, CancellationToken cancellationToken);
}
