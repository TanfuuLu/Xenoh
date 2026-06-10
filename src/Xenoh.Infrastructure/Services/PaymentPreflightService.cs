using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Pre-transfer gate. Runs cheap server-logic checks first (bank config, DB reachability),
/// then a live ping to SePay's user API. Any failure blocks the QR / transfer info so the user
/// never sends money we can't honor (SePay transfers are not refundable programmatically).
/// </summary>
public sealed class PaymentPreflightService(
    HttpClient http,
    IOptions<SePayOptions> options,
    ISePayBankInfo bankInfo,
    ApplicationDbContext db,
    ILogger<PaymentPreflightService> logger
) : IPaymentPreflightService
{
    public async Task<PaymentPreflightResult> CheckAsync(CancellationToken ct)
    {
        var opt = options.Value;

        if (!opt.PreflightEnabled)
            return PaymentPreflightResult.Ok();

        // --- Server-logic status ---

        // Bank info must be present (the user is about to transfer to this account).
        if (IsMissing(bankInfo.BankAccountNumber) ||
            IsMissing(bankInfo.BankAccountName) ||
            IsMissing(bankInfo.BankName))
        {
            return PaymentPreflightResult.Fail(
                PreflightFailureKind.Misconfigured, "SePay bank information is not configured.");
        }

        // DB must be reachable — otherwise the webhook later won't be able to activate the order.
        try
        {
            if (!await db.Database.CanConnectAsync(ct))
                return PaymentPreflightResult.Fail(
                    PreflightFailureKind.ServerLogicUnhealthy, "Database is not reachable.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Payment preflight: database connectivity check failed.");
            return PaymentPreflightResult.Fail(
                PreflightFailureKind.ServerLogicUnhealthy, "Database connectivity check failed.");
        }

        // --- SePay status ---

        if (IsMissing(opt.ApiToken))
            return PaymentPreflightResult.Fail(
                PreflightFailureKind.Misconfigured, "SePay API token is not configured.");

        var url = $"{opt.ApiBaseUrl.TrimEnd('/')}/{opt.HealthProbePath.TrimStart('/')}";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opt.ApiToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(opt.HealthTimeoutSeconds));

            using var resp = await http.SendAsync(req, cts.Token);

            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("Payment preflight: SePay rejected API credentials ({Status}).", resp.StatusCode);
                return PaymentPreflightResult.Fail(
                    PreflightFailureKind.SePayUnreachable, "SePay rejected the API credentials.");
            }

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Payment preflight: SePay returned {Status}.", (int)resp.StatusCode);
                return PaymentPreflightResult.Fail(
                    PreflightFailureKind.SePayUnreachable, $"SePay returned HTTP {(int)resp.StatusCode}.");
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Payment preflight: SePay health check timed out after {Seconds}s.", opt.HealthTimeoutSeconds);
            return PaymentPreflightResult.Fail(
                PreflightFailureKind.SePayUnreachable, "SePay health check timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Payment preflight: SePay is not reachable.");
            return PaymentPreflightResult.Fail(
                PreflightFailureKind.SePayUnreachable, "SePay is not reachable.");
        }

        return PaymentPreflightResult.Ok();
    }

    private static bool IsMissing(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
