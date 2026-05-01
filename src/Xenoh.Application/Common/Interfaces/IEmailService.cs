namespace Xenoh.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken);
}
