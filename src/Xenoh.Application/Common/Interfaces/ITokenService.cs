using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
