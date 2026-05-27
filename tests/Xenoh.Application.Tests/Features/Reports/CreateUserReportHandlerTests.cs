using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Reports.Commands.CreateUserReport;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Reports;

public sealed class CreateUserReportHandlerTests : IdentityHandlerTestBase
{
    private CreateUserReportHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new UserReportRepository(ctx), new UserBlockRepository(ctx), CurrentUser(), CreateUserManager());

    [Fact]
    public async Task Handle_WhenValidReport_CreatesReportRecord()
    {
        await SeedUserAsync(UserId, "reporter@test.com", "Pass123");
        var reported = await SeedUserAsync(Guid.NewGuid(), "reported@test.com", "Pass123");

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = reported.Id,
            Reason = ReportReason.Harassment,
            Details = "This user is harassing me repeatedly."
        }, CancellationToken.None);

        result.ReporterId.Should().Be(UserId);
        result.ReportedUserId.Should().Be(reported.Id);
        result.Reason.Should().Be(ReportReason.Harassment);

        await using var verify = CreateContext();
        var exists = await verify.UserReports.AnyAsync(r =>
            r.ReporterId == UserId && r.ReportedUserId == reported.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenReportingSelf_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = UserId,
            Reason = ReportReason.Spam,
            Details = "Self report"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot report yourself.");
    }

    [Fact]
    public async Task Handle_WhenDetailsWhitespaceOnly_Throws()
    {
        var reportedId = Guid.NewGuid();

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = reportedId,
            Reason = ReportReason.Spam,
            Details = "   "
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Report details are required.");
    }

    [Fact]
    public async Task Handle_WhenReporterNotInUserManager_Throws()
    {
        // Current user (UserId) is NOT seeded in Identity — UserManager won't find them
        var reportedId = Guid.NewGuid();

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = reportedId,
            Reason = ReportReason.Scam,
            Details = "Some scam details"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WhenReportedUserNotInUserManager_Throws()
    {
        await SeedUserAsync(UserId, "reporter2@test.com", "Pass123");

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = Guid.NewGuid(), // unknown user
            Reason = ReportReason.Inappropriate,
            Details = "Inappropriate content."
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Reported user not found.");
    }

    [Fact]
    public async Task Handle_WhenBlockExists_Throws()
    {
        await SeedUserAsync(UserId, "reporter3@test.com", "Pass123");
        var reported = await SeedUserAsync(Guid.NewGuid(), "reported3@test.com", "Pass123");

        await using var seedCtx = CreateContext();
        seedCtx.UserBlocks.Add(new UserBlock { BlockerId = UserId, BlockedId = reported.Id });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreateUserReportCommand
        {
            ReportedUserId = reported.Id,
            Reason = ReportReason.Harassment,
            Details = "Harassment details."
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot report this user.");
    }
}
