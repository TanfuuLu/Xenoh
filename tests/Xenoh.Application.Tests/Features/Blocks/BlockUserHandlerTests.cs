using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Blocks.Commands.BlockUser;
using Xenoh.Application.Features.Blocks.Commands.UnblockUser;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Blocks;

public sealed class BlockUserHandlerTests : HandlerTestBase
{
    private BlockUserHandler CreateBlockHandler(ApplicationDbContext ctx) =>
        new(new UserBlockRepository(ctx), ctx, CurrentUser());

    private UnblockUserHandler CreateUnblockHandler(ApplicationDbContext ctx) =>
        new(new UserBlockRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenBlockingAnotherUser_CreatesBlock()
    {
        var targetId = await SeedTargetUserAsync();

        await using var ctx = CreateContext();
        await CreateBlockHandler(ctx).Handle(new BlockUserCommand { TargetUserId = targetId }, CancellationToken.None);

        await using var verify = CreateContext();
        var exists = await verify.UserBlocks.AnyAsync(b => b.BlockerId == UserId && b.BlockedId == targetId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBlockingSelf_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateBlockHandler(ctx).Handle(new BlockUserCommand { TargetUserId = UserId }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chặn chính mình*");
    }

    [Fact]
    public async Task Handle_WhenTargetUserNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateBlockHandler(ctx).Handle(new BlockUserCommand { TargetUserId = Guid.NewGuid() }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không tồn tại*");
    }

    [Fact]
    public async Task Handle_WhenAlreadyBlocked_IsIdempotent()
    {
        var targetId = await SeedTargetUserAsync();
        await SeedBlockAsync(UserId, targetId);

        await using var ctx = CreateContext();
        var act = () => CreateBlockHandler(ctx).Handle(new BlockUserCommand { TargetUserId = targetId }, CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Unblock_WhenBlockExists_RemovesBlock()
    {
        var targetId = await SeedTargetUserAsync();
        await SeedBlockAsync(UserId, targetId);

        await using var ctx = CreateContext();
        await CreateUnblockHandler(ctx).Handle(new UnblockUserCommand { TargetUserId = targetId }, CancellationToken.None);

        await using var verify = CreateContext();
        var exists = await verify.UserBlocks.AnyAsync(b => b.BlockerId == UserId && b.BlockedId == targetId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Unblock_WhenNoBlockExists_IsIdempotent()
    {
        var targetId = Guid.NewGuid();

        await using var ctx = CreateContext();
        var act = () => CreateUnblockHandler(ctx).Handle(new UnblockUserCommand { TargetUserId = targetId }, CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
    }

    private async Task<Guid> SeedTargetUserAsync()
    {
        var targetId = Guid.NewGuid();
        await using var ctx = CreateContext();
        ctx.ApplicationUsers.Add(new ApplicationUser
        {
            Id = targetId,
            Email = "target@test.com",
            UserName = "target@test.com",
            FirstName = "Target",
            LastName = "User"
        });
        await ctx.SaveChangesAsync();
        return targetId;
    }

    private async Task SeedBlockAsync(Guid blockerId, Guid blockedId)
    {
        await using var ctx = CreateContext();
        ctx.UserBlocks.Add(new UserBlock { BlockerId = blockerId, BlockedId = blockedId });
        await ctx.SaveChangesAsync();
    }
}
