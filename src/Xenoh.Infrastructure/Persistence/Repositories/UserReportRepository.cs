using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Reports;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class UserReportRepository(ApplicationDbContext db) : IUserReportRepository
{
    public Task<UserReport?> FindAsync(Guid reportId, CancellationToken ct = default) =>
        db.UserReports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReviewedBy)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

    public Task<List<UserReportResponse>> GetReportsAsync(ReportStatus? status = null, ReportReason? reason = null, CancellationToken ct = default)
    {
        var query = db.UserReports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReviewedBy)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (reason.HasValue)
            query = query.Where(r => r.Reason == reason.Value);

        return query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new UserReportResponse(
                r.Id,
                r.ReporterId,
                $"{r.Reporter.FirstName} {r.Reporter.LastName}".Trim(),
                r.ReportedUserId,
                $"{r.ReportedUser.FirstName} {r.ReportedUser.LastName}".Trim(),
                r.ReportedUser.Email!,
                r.Reason,
                r.Details,
                r.Status,
                r.AdminNote,
                r.ReviewedById,
                r.ReviewedBy == null ? null : $"{r.ReviewedBy.FirstName} {r.ReviewedBy.LastName}".Trim(),
                r.ReviewedAtUtc,
                r.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task AddAsync(UserReport report, CancellationToken ct = default) =>
        await db.UserReports.AddAsync(report, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
