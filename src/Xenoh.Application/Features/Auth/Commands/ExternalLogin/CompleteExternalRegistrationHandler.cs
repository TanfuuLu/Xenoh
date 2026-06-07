using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Auth.Commands.Register;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed class CompleteExternalRegistrationHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo
) : IRequestHandler<CompleteExternalRegistrationCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(
        CompleteExternalRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var allowedRoles = new[] { UserRole.Individual };
        if (!allowedRoles.Contains(request.Role))
            throw new InvalidOperationException($"Role '{request.Role}' is not allowed. Registration is open to Individual accounts only.");

        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
            return await CreateAuthResponseAsync(user, roles, cancellationToken);

        var addRoleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!addRoleResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));

        roles = await userManager.GetRolesAsync(user);
        return await CreateAuthResponseAsync(user, roles, cancellationToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user,
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        await refreshTokenRepo.AddAsync(new TokenEntity
        {
            Token = tokenService.HashRefreshToken(refreshTokenValue),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, cancellationToken);
        await refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            accessToken,
            refreshTokenValue,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.AvatarUrl,
            roles
        );
    }
}
