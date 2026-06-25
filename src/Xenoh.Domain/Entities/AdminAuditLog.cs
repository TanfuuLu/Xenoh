using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class AdminAuditLog : BaseEntity
{
    public Guid AdminUserId { get; set; }
    public ApplicationUser AdminUser { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public Guid? TargetUserId { get; set; }
    public ApplicationUser? TargetUser { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BeforeSummary { get; set; } = string.Empty;
    public string AfterSummary { get; set; } = string.Empty;
}
