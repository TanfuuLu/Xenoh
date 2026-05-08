using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Reports.Commands.CreateUserReport;

public sealed class CreateUserReportHandler(
    IUserReportRepository reportRepo,
    IUserBlockRepository userBlockRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<CreateUserReportCommand, UserReportResponse>
{
    public async ValueTask<UserReportResponse> Handle(CreateUserReportCommand request, CancellationToken cancellationToken)
    {
        if (request.ReportedUserId == currentUser.UserId)
            throw new InvalidOperationException("You cannot report yourself.");

        if (string.IsNullOrWhiteSpace(request.Details))
            throw new InvalidOperationException("Report details are required.");

        if (await userBlockRepo.IsEitherBlockedAsync(currentUser.UserId, request.ReportedUserId, cancellationToken))
            throw new InvalidOperationException("Cannot report this user.");

        var reporter = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var reported = await userManager.FindByIdAsync(request.ReportedUserId.ToString())
            ?? throw new InvalidOperationException("Reported user not found.");

        var report = new UserReport
        {
            ReporterId = reporter.Id,
            ReportedUserId = reported.Id,
            Reason = request.Reason,
            Details = request.Details.Trim()
        };

        await reportRepo.AddAsync(report, cancellationToken);
        await reportRepo.SaveChangesAsync(cancellationToken);

        return new UserReportResponse(
            report.Id,
            reporter.Id,
            $"{reporter.FirstName} {reporter.LastName}".Trim(),
            reported.Id,
            $"{reported.FirstName} {reported.LastName}".Trim(),
            reported.Email!,
            report.Reason,
            report.Details,
            report.Status,
            report.AdminNote,
            null,
            null,
            null,
            report.CreatedAt);
    }
}
