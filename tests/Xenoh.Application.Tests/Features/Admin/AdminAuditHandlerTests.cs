using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Admin;
using Xenoh.Application.Features.Reports.Commands.ReviewReport;
using Xenoh.Application.Features.Reports.Commands.SetUserSuspension;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Admin;

public sealed class AdminAuditHandlerTests : IdentityHandlerTestBase
{
    [Fact]
    public async Task SetUserSuspension_CreatesAuditLog()
    {
        await SeedRolesAsync();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        var targetId = Guid.NewGuid();
        await SeedUserAsync(targetId, "target@test.com", "password");
        await using var ctx = CreateContext();
        var handler = new SetUserSuspensionHandler(
            CreateUserManager(), CurrentUser(), new RefreshTokenRepository(ctx), ctx);

        await handler.Handle(new SetUserSuspensionCommand(targetId, true), CancellationToken.None);

        ctx.AdminAuditLogs.Single().Action.Should().Be(AdminAudit.SuspendUser);
        ctx.AdminAuditLogs.Single().TargetUserId.Should().Be(targetId);
    }

    [Fact]
    public async Task ReviewReport_CreatesAuditLog()
    {
        await SeedRolesAsync();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        var reporterId = Guid.NewGuid();
        var reportedId = Guid.NewGuid();
        await SeedUserAsync(reporterId, "reporter@test.com", "password");
        await SeedUserAsync(reportedId, "reported@test.com", "password");
        Guid reportId;
        await using (var seed = CreateContext())
        {
            var report = new UserReport
            {
                ReporterId = reporterId,
                ReportedUserId = reportedId,
                Reason = ReportReason.Spam,
                Details = "Spam"
            };
            seed.UserReports.Add(report);
            await seed.SaveChangesAsync();
            reportId = report.Id;
        }

        await using var ctx = CreateContext();
        var handler = new ReviewReportHandler(new UserReportRepository(ctx), CurrentUser(), CreateUserManager(), ctx);

        await handler.Handle(
            new ReviewReportCommand
            {
                ReportId = reportId,
                Status = ReportStatus.Resolved,
                AdminNote = "Reviewed"
            },
            CancellationToken.None);

        ctx.AdminAuditLogs.Single().Action.Should().Be(AdminAudit.ReviewReport);
        ctx.AdminAuditLogs.Single().TargetUserId.Should().Be(reportedId);
    }
}
