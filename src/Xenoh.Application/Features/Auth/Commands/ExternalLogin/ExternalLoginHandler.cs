using System.Security.Cryptography;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed class ExternalLoginHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db
) : IRequestHandler<ExternalLoginCommand, ExternalLoginTicketResponse>
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(5);

    public async ValueTask<ExternalLoginTicketResponse> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("External provider did not return an email address.");

        var user = await userManager.FindByLoginAsync(request.Provider, request.ProviderKey);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? "Xenoh" : request.FirstName.Trim(),
                    LastName = request.LastName?.Trim() ?? "User",
                    AvatarUrl = request.AvatarUrl,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            var loginResult = await userManager.AddLoginAsync(
                user,
                new UserLoginInfo(request.Provider, request.ProviderKey, request.Provider));

            if (!loginResult.Succeeded && !loginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                throw new InvalidOperationException(string.Join("; ", loginResult.Errors.Select(e => e.Description)));
        }

        var ticket = GenerateTicket();
        db.ExternalAuthTickets.Add(new ExternalAuthTicket
        {
            UserId = user.Id,
            TicketHash = ExternalAuthTicketHasher.Hash(ticket),
            ExpiresAt = DateTime.UtcNow.Add(TicketLifetime)
        });

        await db.SaveChangesAsync(cancellationToken);

        return new ExternalLoginTicketResponse(ticket);
    }

    private static string GenerateTicket()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
