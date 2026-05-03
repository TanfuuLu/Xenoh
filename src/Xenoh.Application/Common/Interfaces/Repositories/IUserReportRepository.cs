using Xenoh.Application.Features.Reports;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IUserReportRepository
{
    Task<UserReport?> FindAsync(Guid reportId, CancellationToken ct = default);
    Task<List<UserReportResponse>> GetReportsAsync(ReportStatus? status = null, ReportReason? reason = null, CancellationToken ct = default);
    Task AddAsync(UserReport report, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
