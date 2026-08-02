namespace Xenoh.Application.Features.Subscriptions;

public static class SubscriptionContract
{
    public const string CurrentTermsVersion = "2026-08-02";

    public static void EnsureCurrentTermsAccepted(bool accepted, string? version)
    {
        if (!accepted || !string.Equals(version, CurrentTermsVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("You must accept the current Terms of Service before creating a payment order.");
    }
}
