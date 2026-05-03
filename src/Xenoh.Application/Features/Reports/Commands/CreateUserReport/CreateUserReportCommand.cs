using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Reports.Commands.CreateUserReport;

public sealed record CreateUserReportCommand : IRequest<UserReportResponse>
{
    public Guid ReportedUserId { get; init; }
    public ReportReason Reason { get; init; }

    [Required]
    [MaxLength(2000)]
    public string Details { get; init; } = string.Empty;
}
