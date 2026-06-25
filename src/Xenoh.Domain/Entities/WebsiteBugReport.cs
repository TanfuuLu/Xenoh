using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class WebsiteBugReport : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string? BrowserInfo { get; set; }
    public WebsiteBugReportSeverity Severity { get; set; } = WebsiteBugReportSeverity.Medium;
    public WebsiteBugReportStatus Status { get; set; } = WebsiteBugReportStatus.Open;

    public string? AdminNote { get; set; }
    public Guid? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
