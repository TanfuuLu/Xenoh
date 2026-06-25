using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Admin;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Reports.Commands.ReviewReport;

public sealed class ReviewReportHandler(
    IUserReportRepository reportRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db
) : IRequestHandler<ReviewReportCommand, UserReportResponse>
{
    public async ValueTask<UserReportResponse> Handle(ReviewReportCommand request, CancellationToken cancellationToken)
    {
        if (request.Status == ReportStatus.Pending)
            throw new InvalidOperationException("Report review status must be Resolved or Dismissed.");

        var report = await reportRepo.FindAsync(request.ReportId, cancellationToken)
            ?? throw new InvalidOperationException("Report not found.");
        var admin = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("Admin not found.");

        var before = $"Status={report.Status}; AdminNote={report.AdminNote ?? "none"}";
        report.Status = request.Status;
        report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
        report.ReviewedById = admin.Id;
        report.ReviewedAtUtc = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        var after = $"Status={report.Status}; AdminNote={report.AdminNote ?? "none"}";

        AdminAudit.Add(
            db,
            currentUser.UserId,
            AdminAudit.ReviewReport,
            nameof(UserReport),
            report.Id,
            report.ReportedUserId,
            report.AdminNote ?? $"Report marked {report.Status}.",
            before,
            after);

        await reportRepo.SaveChangesAsync(cancellationToken);

        return new UserReportResponse(
            report.Id,
            report.ReporterId,
            $"{report.Reporter.FirstName} {report.Reporter.LastName}".Trim(),
            report.ReportedUserId,
            $"{report.ReportedUser.FirstName} {report.ReportedUser.LastName}".Trim(),
            report.ReportedUser.Email!,
            report.Reason,
            report.Details,
            report.Status,
            report.AdminNote,
            report.ReviewedById,
            $"{admin.FirstName} {admin.LastName}".Trim(),
            report.ReviewedAtUtc,
            report.CreatedAt);
    }
}
