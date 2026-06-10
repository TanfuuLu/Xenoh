namespace Xenoh.Application.Common.Interfaces;

public enum PreflightFailureKind
{
    None = 0,
    SePayUnreachable = 1,
    ServerLogicUnhealthy = 2,
    Misconfigured = 3
}

/// <summary>
/// Result of the pre-transfer preflight. When <see cref="Healthy"/> is false the caller must
/// NOT show the QR / bank-transfer info, because SePay won't be able to refund if the money
/// can't be honored.
/// </summary>
public sealed record PaymentPreflightResult(bool Healthy, PreflightFailureKind Kind, string? Reason)
{
    public static PaymentPreflightResult Ok() => new(true, PreflightFailureKind.None, null);

    public static PaymentPreflightResult Fail(PreflightFailureKind kind, string reason) =>
        new(false, kind, reason);
}

/// <summary>
/// Verifies, before a user is shown payment instructions, that (a) SePay is reachable and
/// (b) the server can actually honor an incoming payment. Since SePay bank transfers cannot be
/// refunded programmatically, we fail closed BEFORE the user transfers money.
/// </summary>
public interface IPaymentPreflightService
{
    Task<PaymentPreflightResult> CheckAsync(CancellationToken ct);
}
