using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Auth;

public sealed class TemporaryFeatureBypassClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || identity.IsAuthenticated is not true)
            return Task.FromResult(principal);

        // TEMP TEST BYPASS: let authenticated users test individual and coach flows.
        AddRoleIfMissing(identity, UserRole.Individual);
        AddRoleIfMissing(identity, UserRole.Coach);

        return Task.FromResult(principal);
    }

    private static void AddRoleIfMissing(ClaimsIdentity identity, string role)
    {
        if (identity.HasClaim(ClaimTypes.Role, role))
            return;

        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }
}
