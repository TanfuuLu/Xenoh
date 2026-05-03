using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Reports.Queries.GetReports;

public sealed record GetReportsQuery(ReportStatus? Status = null, ReportReason? Reason = null) : IRequest<List<UserReportResponse>>;
