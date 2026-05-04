using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Auth.Commands.Register;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed class ExchangeExternalLoginTicketHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo
) : IRequestHandler<ExchangeExternalLoginTicketCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(
        ExchangeExternalLoginTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticketHash = ExternalAuthTicketHasher.Hash(request.Ticket);
        var now = DateTime.UtcNow;

        var ticket = await db.ExternalAuthTickets
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TicketHash == ticketHash, cancellationToken)
            ?? throw new InvalidOperationException("Invalid external login ticket.");

        if (ticket.UsedAt is not null || ticket.ExpiresAt <= now)
            throw new InvalidOperationException("Invalid external login ticket.");

        ticket.UsedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        // Ensure every social-login user has a role (Individual by default)
        var existingRoles = await userManager.GetRolesAsync(ticket.User);
        if (existingRoles.Count == 0)
            await userManager.AddToRoleAsync(ticket.User, UserRole.Individual);

        return await CreateAuthResponseAsync(ticket.User, cancellationToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshToken = new TokenEntity
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokenRepo.AddAsync(refreshToken, cancellationToken);
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
