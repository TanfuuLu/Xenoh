using Mediator;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Reports.Queries.GetReports;

public sealed class GetReportsHandler(IUserReportRepository reportRepo)
    : IRequestHandler<GetReportsQuery, List<UserReportResponse>>
{
    public async ValueTask<List<UserReportResponse>> Handle(GetReportsQuery request, CancellationToken cancellationToken) =>
        await reportRepo.GetReportsAsync(request.Status, request.Reason, cancellationToken);
}
