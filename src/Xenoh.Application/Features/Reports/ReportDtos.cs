using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Reports;

public sealed record UserReportResponse(
    Guid Id,
    Guid ReporterId,
    string ReporterName,
    Guid ReportedUserId,
    string ReportedUserName,
    string ReportedUserEmail,
    ReportReason Reason,
    string Details,
    ReportStatus Status,
    string? AdminNote,
    Guid? ReviewedById,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAt,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    bool IsReportedUserSuspended
);
