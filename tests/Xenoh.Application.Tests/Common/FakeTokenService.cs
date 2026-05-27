using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeTokenService : ITokenService
{
    public string GenerateAccessToken(ApplicationUser user, IList<string> roles) => "test-access-token";
    public string GenerateRefreshToken() => "test-refresh-token";
    public string HashRefreshToken(string refreshToken) => refreshToken;
}
