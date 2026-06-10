namespace Xenoh.Application.Common.Exceptions;

/// <summary>
/// Thrown when the pre-transfer preflight fails — SePay is unreachable or the server cannot
/// honor an incoming payment. The API maps this to HTTP 503 so the client knows to retry later
/// and no payment instructions are shown.
/// </summary>
public sealed class PaymentServiceUnavailableException(string reason) : Exception(reason);
