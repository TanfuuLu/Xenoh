using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Admin;

public static class AdminAudit
{
    public const string ReviewReport = "ReviewReport";
    public const string SuspendUser = "SuspendUser";
    public const string UnsuspendUser = "UnsuspendUser";
    public const string AdjustSubscription = "AdjustSubscription";
    public const string CreatePromotionCode = "CreatePromotionCode";
    public const string UpdatePromotionCode = "UpdatePromotionCode";
    public const string DeletePromotionCode = "DeletePromotionCode";

    public static AdminAuditLog Add(
        IApplicationDbContext db,
        Guid adminUserId,
        string action,
        string targetType,
        Guid? targetId,
        Guid? targetUserId,
        string reason,
        string beforeSummary,
        string afterSummary)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Admin action reason is required.");

        var log = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetUserId = targetUserId,
            Reason = reason.Trim(),
            BeforeSummary = beforeSummary,
            AfterSummary = afterSummary
        };

        db.AdminAuditLogs.Add(log);
        return log;
    }
}
