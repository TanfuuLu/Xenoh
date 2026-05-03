using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class UserReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public ApplicationUser Reporter { get; set; } = null!;

    public Guid ReportedUserId { get; set; }
    public ApplicationUser ReportedUser { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public string Details { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public string? AdminNote { get; set; }
    public Guid? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
