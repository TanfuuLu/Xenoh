using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Services;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.ExerciseTemplates;

public sealed class CreateCustomExerciseTemplateHandlerTests : HandlerTestBase
{
    private CreateCustomExerciseTemplateHandler CreateHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser(), new SubscriptionService(new SubscriptionRepository(ctx)));

    [Fact]
    public async Task Handle_WhenProUser_CreatesTemplate()
    {
        await SeedProSubscriptionAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreateCustomExerciseTemplateCommand
        {
            Name = "Custom Squat",
            PrimaryMuscleGroup = MuscleGroup.Quads,
            ExerciseKind = ExerciseKind.Strength
        }, CancellationToken.None);

        result.Name.Should().Be("Custom Squat");
        result.IsCustom.Should().BeTrue();
        result.OwnerId.Should().Be(UserId);

        await using var verify = CreateContext();
        var exists = await verify.ExerciseTemplates.AnyAsync(t => t.Name == "Custom Squat" && t.OwnerId == UserId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFreeUserHasFewerThanTen_CreatesTemplate()
    {
        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreateCustomExerciseTemplateCommand
        {
            Name = "My Exercise",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength
        }, CancellationToken.None);

        result.Name.Should().Be("My Exercise");
        result.IsCustom.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFreeUserAlreadyHasTen_Throws()
    {
        await using var ctx = CreateContext();
        for (var index = 0; index < 10; index++)
        {
            ctx.ExerciseTemplates.Add(new ExerciseTemplate
            {
                Name = $"Custom Exercise {index + 1}",
                OwnerId = UserId,
                PrimaryMuscleGroup = MuscleGroup.Chest,
                ExerciseKind = ExerciseKind.Strength
            });
        }
        await ctx.SaveChangesAsync();

        var act = () => CreateHandler(ctx).Handle(new CreateCustomExerciseTemplateCommand
        {
            Name = "Exercise Eleven",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*up to 10 custom exercises*");
    }

    [Fact]
    public async Task Handle_WhenPrimaryIsInSecondary_FiltersIt()
    {
        await SeedProSubscriptionAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreateCustomExerciseTemplateCommand
        {
            Name = "Compound Move",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Triceps],
            ExerciseKind = ExerciseKind.Strength
        }, CancellationToken.None);

        result.SecondaryMuscleGroups.Should().NotContain("Chest");
        result.SecondaryMuscleGroups.Should().Contain("Triceps");
    }

    private async Task SeedProSubscriptionAsync(Guid userId)
    {
        await using var ctx = CreateContext();
        ctx.UserSubscriptions.Add(new UserSubscription
        {
            UserId = userId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await ctx.SaveChangesAsync();
    }
}
