using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed class VerifyAccountDeletionHandler(
    IApplicationDbContext db,
    IAccountDeletionService accountDeletionService) : IRequestHandler<VerifyAccountDeletionCommand>
{
    public async ValueTask<Unit> Handle(VerifyAccountDeletionCommand request, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var deletion = await db.AccountDeletionRequests.SingleOrDefaultAsync(x => x.VerificationTokenHash == hash, ct)
            ?? throw new InvalidOperationException("Invalid or expired account deletion link.");
        if (deletion.Status == AccountDeletionStatus.Completed) return Unit.Value;
        if (deletion.Status != AccountDeletionStatus.Pending || deletion.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired account deletion link.");
        deletion.Status = AccountDeletionStatus.Verified;
        deletion.VerifiedAt = DateTime.UtcNow;
        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog { AccountDeletionRequest = deletion, EventType = "Verified" });
        if (deletion.UserId is null) throw new InvalidOperationException("Account deletion request has no account.");
        await accountDeletionService.DeleteAccountAsync(deletion.UserId.Value, deletion, accessToken: null, ct);
        return Unit.Value;
    }
}
