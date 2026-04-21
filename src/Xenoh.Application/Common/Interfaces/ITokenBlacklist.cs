namespace Xenoh.Application.Common.Interfaces;

public interface ITokenBlacklist
{
    void RevokeToken(string token);
    bool IsTokenRevoked(string token);
}
