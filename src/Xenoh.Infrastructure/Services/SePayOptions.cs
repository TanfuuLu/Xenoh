namespace Xenoh.Infrastructure.Services;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public string ApiKey { get; init; } = string.Empty;
    public string BankAccountNumber { get; init; } = string.Empty;
    public string BankAccountName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
}
