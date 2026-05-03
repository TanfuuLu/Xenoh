namespace Xenoh.Application.Common.Interfaces;

public interface ISePayWebhookVerifier
{
    bool Verify(string? authorizationHeader);
}
