using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Reports.Commands.ReviewReport;

public sealed record ReviewReportCommand : IRequest<UserReportResponse>
{
    public Guid ReportId { get; init; }
    public ReportStatus Status { get; init; }

    [MaxLength(2000)]
    public string? AdminNote { get; init; }
}
