using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class AccountDeletionRequest : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string VerificationTokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AccountDeletionStatus Status { get; set; } = AccountDeletionStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime RetainUntil { get; set; }
    public string? FailureReason { get; set; }
}
