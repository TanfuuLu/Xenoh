using System.Security.Cryptography;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed class SendForgotPasswordCodeHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IEmailService emailService
) : IRequestHandler<SendForgotPasswordCodeCommand>
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public async ValueTask<Unit> Handle(SendForgotPasswordCodeCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
            return Unit.Value;

        var now = DateTime.UtcNow;
        var recentCode = await db.PasswordResetCodes
            .AsNoTracking()
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentCode is not null && recentCode.CreatedAt > now.Subtract(ResendCooldown))
            return Unit.Value;

        var activeCodes = await db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var activeCode in activeCodes)
            activeCode.UsedAt = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            UserId = user.Id,
            Email = user.Email!,
            CodeHash = PasswordResetCodeHasher.Hash(user.Email!, code, user),
            ResetToken = resetToken,
            ExpiresAt = now.Add(CodeLifetime),
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordResetCodeAsync(
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            code,
            cancellationToken);

        return Unit.Value;
    }
}
