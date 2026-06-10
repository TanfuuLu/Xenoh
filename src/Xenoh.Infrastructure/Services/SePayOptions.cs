namespace Xenoh.Infrastructure.Services;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    // Shared secret SePay sends in the inbound webhook's "Authorization: Apikey ..." header.
    public string ApiKey { get; init; } = string.Empty;
    public string BankAccountNumber { get; init; } = string.Empty;
    public string BankAccountName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;

    // Bearer token for SePay's user API (generated in the SePay dashboard, NOT the webhook ApiKey).
    // Used only for the pre-transfer health probe.
    public string ApiToken { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://my.sepay.vn/userapi";
    public string HealthProbePath { get; init; } = "/transactions/count";

    // When false, the pre-transfer preflight is skipped entirely (e.g. local dev).
    public bool PreflightEnabled { get; init; } = true;
    public int HealthTimeoutSeconds { get; init; } = 5;
}
