using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class SePayBankInfo(IOptions<SePayOptions> options) : ISePayBankInfo
{
    public string BankAccountNumber => options.Value.BankAccountNumber;
    public string BankAccountName => options.Value.BankAccountName;
    public string BankName => options.Value.BankName;
}
