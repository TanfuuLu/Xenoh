using FluentAssertions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Moderation;

public sealed class UserReportRepositoryTests : HandlerTestBase
{
    [Fact]
    public async Task GetReportsAsync_FiltersByStatusAndReason()
    {
        var (reporterId, reportedId) = await SeedUsersAsync();
        await using var seedCtx = CreateContext();
        seedCtx.UserReports.Add(new UserReport
        {
            ReporterId = reporterId,
            ReportedUserId = reportedId,
            Reason = ReportReason.Spam,
            Details = "Spam messages",
            Status = ReportStatus.Pending
        });
        seedCtx.UserReports.Add(new UserReport
        {
            ReporterId = reporterId,
            ReportedUserId = reportedId,
            Reason = ReportReason.Harassment,
            Details = "Resolved old report",
            Status = ReportStatus.Resolved
        });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var reports = await new UserReportRepository(ctx).GetReportsAsync(ReportStatus.Pending, ReportReason.Spam);

        reports.Should().ContainSingle();
        reports[0].Reason.Should().Be(ReportReason.Spam);
        reports[0].Status.Should().Be(ReportStatus.Pending);
    }

    private async Task<(Guid User1Id, Guid User2Id)> SeedUsersAsync()
    {
        await using var ctx = CreateContext();
        var user1 = new ApplicationUser
        {
            FirstName = "Coach",
            LastName = "User",
            Email = $"{Guid.NewGuid()}@example.test",
            UserName = $"{Guid.NewGuid()}@example.test"
        };
        var user2 = new ApplicationUser
        {
            FirstName = "Client",
            LastName = "User",
            Email = $"{Guid.NewGuid()}@example.test",
            UserName = $"{Guid.NewGuid()}@example.test"
        };
        ctx.Users.AddRange(user1, user2);
        await ctx.SaveChangesAsync();
        return (user1.Id, user2.Id);
    }
}
