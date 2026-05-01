using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class SmtpEmailService(IOptions<SmtpOptions> options) : IEmailService
{
    private readonly SmtpOptions smtpOptions = options.Value;

    public async Task SendPasswordResetCodeAsync(
        string toEmail,
        string fullName,
        string code,
        CancellationToken cancellationToken)
    {
        ValidateOptions();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtpOptions.FromName, smtpOptions.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your Xenoh password reset code";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildPasswordResetTemplate(fullName, code),
            TextBody = $"Your Xenoh password reset code is {code}. It expires in 10 minutes."
        }.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = smtpOptions.EnableSsl
            ? SecureSocketOptions.Auto
            : SecureSocketOptions.None;

        await client.ConnectAsync(smtpOptions.Host, smtpOptions.Port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(smtpOptions.Username, smtpOptions.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(smtpOptions.Host) ||
            string.IsNullOrWhiteSpace(smtpOptions.Username) ||
            string.IsNullOrWhiteSpace(smtpOptions.Password) ||
            string.IsNullOrWhiteSpace(smtpOptions.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are not configured.");
        }
    }

    private static string BuildPasswordResetTemplate(string fullName, string code)
    {
        var safeName = string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim();

        return $$"""
<!doctype html>
<html>
<body style="margin:0;background:#efe4d5;padding:32px 16px;font-family:Georgia,'Times New Roman',serif;color:#2e2218;">
  <div style="max-width:560px;margin:0 auto;background:#fffaf3;border:1px solid #c1aa95;border-radius:12px;overflow:hidden;box-shadow:0 18px 44px rgba(88,63,46,.18);">
    <div style="background:#876f5e;color:#fefbf4;padding:28px 32px;">
      <div style="font-size:26px;font-weight:700;letter-spacing:.02em;">Xenoh</div>
      <div style="font-family:Arial,sans-serif;font-size:13px;margin-top:6px;color:#f7ead8;">Password reset code</div>
    </div>
    <div style="padding:32px;">
      <h1 style="margin:0 0 12px;font-size:30px;line-height:1.1;color:#2e2218;">Reset your password</h1>
      <p style="margin:0 0 22px;font-family:Arial,sans-serif;font-size:15px;line-height:1.6;color:#564537;">
        Hi {{safeName}}, use this code to reset your Xenoh password. It expires in 10 minutes.
      </p>
      <div style="margin:28px 0;padding:22px;border:1px solid #e6cfba;background:#f7ead8;border-radius:10px;text-align:center;">
        <div style="font-family:'Courier New',monospace;font-size:36px;letter-spacing:8px;font-weight:700;color:#583f2e;">{{code}}</div>
      </div>
      <p style="margin:0;font-family:Arial,sans-serif;font-size:13px;line-height:1.6;color:#6e6050;">
        If you did not request this, you can safely ignore this email.
      </p>
    </div>
  </div>
</body>
</html>
""";
    }
}
