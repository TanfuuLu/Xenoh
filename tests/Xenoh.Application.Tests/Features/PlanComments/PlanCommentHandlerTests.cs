using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.PlanComments.Commands.AddPlanComment;
using Xenoh.Application.Features.PlanComments.Commands.DeletePlanComment;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.PlanComments;

public sealed class PlanCommentHandlerTests : HandlerTestBase
{
    private AddPlanCommentHandler CreateAddHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser(), new FakeNotificationService(), new FakeCommentRealtimeService());

    private DeletePlanCommentHandler CreateDeleteHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser(), new FakeCommentRealtimeService());

    [Fact]
    public async Task AddComment_WhenPlanExists_CreatesComment()
    {
        var planId = await SeedUserAndPlanAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateAddHandler(ctx).Handle(new AddPlanCommentCommand
        {
            PlanId = planId,
            Content = "Great plan!"
        }, CancellationToken.None);

        result.Content.Should().Be("Great plan!");
        result.AuthorId.Should().Be(UserId);

        await using var verify = CreateContext();
        var count = await verify.PlanComments.CountAsync(c => c.PlanId == planId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddComment_WhenPlanNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateAddHandler(ctx).Handle(new AddPlanCommentCommand
        {
            PlanId = Guid.NewGuid(),
            Content = "Comment on nothing"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    [Fact]
    public async Task DeleteComment_WhenAuthorDeletes_RemovesComment()
    {
        var planId = await SeedUserAndPlanAsync(UserId);
        var commentId = await SeedCommentAsync(planId, UserId, "To be deleted");

        await using var ctx = CreateContext();
        await CreateDeleteHandler(ctx).Handle(new DeletePlanCommentCommand(planId, commentId), CancellationToken.None);

        await using var verify = CreateContext();
        var exists = await verify.PlanComments.AnyAsync(c => c.Id == commentId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteComment_WhenNotAuthor_Throws()
    {
        var otherUserId = Guid.NewGuid();
        var planId = await SeedUserAndPlanAsync(UserId);
        var commentId = await SeedCommentAsync(planId, otherUserId, "Someone else's comment");

        await using var ctx = CreateContext();
        var act = () => CreateDeleteHandler(ctx).Handle(new DeletePlanCommentCommand(planId, commentId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*own comments*");
    }

    private async Task<Guid> SeedUserAndPlanAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        ctx.ApplicationUsers.Add(new ApplicationUser { Id = ownerId, Email = "owner@test.com", UserName = "owner@test.com", FirstName = "Owner", LastName = "User" });
        var plan = new Plan { Name = "Test Plan", OwnerId = ownerId, PlanType = PlanType.Self, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27)) };
        ctx.Plans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    private async Task<Guid> SeedCommentAsync(Guid planId, Guid authorId, string content)
    {
        await using var ctx = CreateContext();
        var comment = new PlanComment { PlanId = planId, AuthorId = authorId, Content = content };
        ctx.PlanComments.Add(comment);
        await ctx.SaveChangesAsync();
        return comment.Id;
    }
}
