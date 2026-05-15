namespace Xenoh.Application.Common.Interfaces;

public interface ITokenBlacklist
{
    Task RevokeTokenAsync(string token);
    Task<bool> IsTokenRevokedAsync(string token);
}
