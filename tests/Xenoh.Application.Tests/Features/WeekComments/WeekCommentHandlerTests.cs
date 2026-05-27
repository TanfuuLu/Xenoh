using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.WeekComments.Commands.AddWeekComment;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.WeekComments;

public sealed class WeekCommentHandlerTests : HandlerTestBase
{
    private AddWeekCommentHandler CreateHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser(), new FakeNotificationService(), new FakeCommentRealtimeService());

    [Fact]
    public async Task Handle_WhenWeekExists_CreatesComment()
    {
        var weekId = await SeedWeekAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new AddWeekCommentCommand
        {
            WeeklyWorkoutId = weekId,
            Content = "Great week!"
        }, CancellationToken.None);

        result.Content.Should().Be("Great week!");
        result.AuthorId.Should().Be(UserId);

        await using var verify = CreateContext();
        var count = await verify.WeeklyWorkoutComments.CountAsync(c => c.WeeklyWorkoutId == weekId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenWeekNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new AddWeekCommentCommand
        {
            WeeklyWorkoutId = Guid.NewGuid(),
            Content = "Comment on nothing"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Week not found.");
    }

    private async Task<Guid> SeedWeekAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        ctx.ApplicationUsers.Add(new ApplicationUser
        {
            Id = ownerId,
            Email = "user@test.com",
            UserName = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        });

        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(27)
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        };

        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        await ctx.SaveChangesAsync();

        return week.Id;
    }
}
