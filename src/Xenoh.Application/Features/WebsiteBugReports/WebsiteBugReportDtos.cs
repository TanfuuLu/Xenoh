using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.WebsiteBugReports;

public sealed record WebsiteBugReportResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    string Title,
    string Description,
    string? PageUrl,
    string? BrowserInfo,
    WebsiteBugReportSeverity Severity,
    WebsiteBugReportStatus Status,
    string? AdminNote,
    Guid? ReviewedById,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAt);
