using System.Security.Cryptography;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed class RequestAccountDeletionHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext db, IEmailService email, IConfiguration config) : IRequestHandler<RequestAccountDeletionCommand>
{
    public async ValueTask<Unit> Handle(RequestAccountDeletionCommand request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalized);
        if (user is null) return Unit.Value;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        foreach (var pending in await db.AccountDeletionRequests.Where(x => x.UserId == user.Id && x.Status == Xenoh.Domain.Enums.AccountDeletionStatus.Pending).ToListAsync(ct)) pending.Status = Xenoh.Domain.Enums.AccountDeletionStatus.Failed;
        var deletion = new AccountDeletionRequest { Email = normalized, UserId = user.Id, VerificationTokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))), ExpiresAt = now.AddHours(24), RetainUntil = now.AddYears(7) };
        db.AccountDeletionRequests.Add(deletion);
        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog { AccountDeletionRequest = deletion, EventType = "Requested" });
        await db.SaveChangesAsync(ct);
        var baseUrl = config["Authentication:FrontendUrl"]?.TrimEnd('/') ?? "https://xenoh.online";
        await email.SendAccountDeletionVerificationAsync(user.Email!, $"{user.FirstName} {user.LastName}", $"{baseUrl}/account-deletion/verify?token={token}", ct);
        return Unit.Value;
    }
}
