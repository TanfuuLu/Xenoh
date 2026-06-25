using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Admin;
using Xenoh.Application.Features.WebsiteBugReports;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Tests.Features.WebsiteBugReports;

public sealed class WebsiteBugReportHandlerTests : IdentityHandlerTestBase
{
    [Fact]
    public async Task CreateWebsiteBugReport_CreatesOpenReport()
    {
        await SeedUserAsync(UserId, "reporter@test.com", "password");
        await using var ctx = CreateContext();
        var handler = new CreateWebsiteBugReportHandler(ctx, CurrentUser());

        var result = await handler.Handle(new CreateWebsiteBugReportCommand
        {
            Title = "Broken dashboard",
            Description = "The dashboard chart fails to render.",
            PageUrl = "https://xenoh.test/dashboard",
            BrowserInfo = "Test browser",
            Severity = WebsiteBugReportSeverity.High
        }, CancellationToken.None);

        result.Status.Should().Be(WebsiteBugReportStatus.Open);
        result.Severity.Should().Be(WebsiteBugReportSeverity.High);
        ctx.WebsiteBugReports.Single().Title.Should().Be("Broken dashboard");
    }

    [Fact]
    public async Task ReviewWebsiteBugReport_UpdatesStatusAndCreatesAuditLog()
    {
        await SeedRolesAsync();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        var reporterId = Guid.NewGuid();
        await SeedUserAsync(reporterId, "reporter@test.com", "password");
        Guid bugReportId;
        await using (var seed = CreateContext())
        {
            var create = new CreateWebsiteBugReportHandler(seed, new FakeCurrentUserService(reporterId));
            var created = await create.Handle(new CreateWebsiteBugReportCommand
            {
                Title = "Broken profile",
                Description = "Profile save fails.",
                Severity = WebsiteBugReportSeverity.Medium
            }, CancellationToken.None);
            bugReportId = created.Id;
        }

        await using var ctx = CreateContext();
        var handler = new ReviewWebsiteBugReportHandler(ctx, CurrentUser(), CreateUserManager());

        var result = await handler.Handle(new ReviewWebsiteBugReportCommand
        {
            BugReportId = bugReportId,
            Status = WebsiteBugReportStatus.Resolved,
            AdminNote = "Fixed"
        }, CancellationToken.None);

        result.Status.Should().Be(WebsiteBugReportStatus.Resolved);
        ctx.AdminAuditLogs.Single().Action.Should().Be("ReviewWebsiteBugReport");
        ctx.AdminAuditLogs.Single().TargetId.Should().Be(bugReportId);
    }
}
