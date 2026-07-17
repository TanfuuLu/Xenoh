using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public sealed class AccountDeletionAuditLog : BaseEntity
{
    public Guid AccountDeletionRequestId { get; set; }
    public AccountDeletionRequest AccountDeletionRequest { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
