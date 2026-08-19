using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Nutrition;
using Xenoh.Application.Features.Nutrition.MealPlans;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Nutrition;

public sealed class MealPlanHandlerTests : HandlerTestBase
{
    private readonly Guid CoachId = Guid.NewGuid();
    private readonly Guid StrangerId = Guid.NewGuid();
    private readonly DateOnly Date = new(2026, 6, 25);

    [Fact]
    public async Task Upsert_WhenUserOwnsPlan_CreatesDailyMealPlan()
    {
        var foodId = await SeedBaseDataAsync();

        await using var ctx = CreateContext();
        var result = await CreateUpsertHandler(ctx, UserId).Handle(CreateCommand(foodId), CancellationToken.None);

        result.UserId.Should().Be(UserId);
        result.Date.Should().Be(Date);
        result.Meals.Should().ContainSingle();
        result.TotalItemCount.Should().Be(1);
        result.PlannedTotals.Calories.Should().Be(150);
    }

    [Fact]
    public async Task Upsert_WhenDateAlreadyHasUncheckedMeals_ReplacesMealPlan()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
            await CreateUpsertHandler(setup, UserId).Handle(CreateCommand(foodId), CancellationToken.None);

        var replacement = new UpsertMealPlanForDateCommand(
            Date,
            null,
            [
                new UpsertMealPlanMealRequest(
                    null,
                    "Lunch",
                    0,
                    [new UpsertMealPlanItemRequest(null, foodId, 0, 200m, null, null)])
            ]);

        await using var ctx = CreateContext();
        var result = await CreateUpsertHandler(ctx, UserId).Handle(replacement, CancellationToken.None);

        result.Meals.Should().ContainSingle();
        result.Meals.Single().Name.Should().Be("Lunch");
        result.PlannedTotals.Calories.Should().Be(200);
        (await ctx.MealPlanMeals.CountAsync()).Should().Be(1);
        (await ctx.MealPlanItems.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Upsert_WhenActiveCoachEditsClient_CreatesDailyMealPlan()
    {
        var foodId = await SeedBaseDataAsync(withCoachRelationship: true);

        await using var ctx = CreateContext();
        var result = await CreateUpsertHandler(ctx, CoachId).Handle(CreateCommand(foodId, UserId), CancellationToken.None);

        result.UserId.Should().Be(UserId);
        result.Meals.Should().ContainSingle();
        // Authorship is what lets a disconnect tell the coach's planning from the client's.
        (await ctx.MealPlanDays.SingleAsync()).CreatedByUserId.Should().Be(CoachId);
    }

    [Fact]
    public async Task Upsert_WhenClientEditsTheirCoachsPlan_TakesOverAuthorship()
    {
        var foodId = await SeedBaseDataAsync(withCoachRelationship: true);
        await using (var setup = CreateContext())
            await CreateUpsertHandler(setup, CoachId).Handle(CreateCommand(foodId, UserId), CancellationToken.None);

        await using var ctx = CreateContext();
        await CreateUpsertHandler(ctx, UserId).Handle(CreateCommand(foodId), CancellationToken.None);

        (await ctx.MealPlanDays.SingleAsync()).CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Get_WhenUnrelatedUserRequestsPlan_Throws()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
            await CreateUpsertHandler(setup, UserId).Handle(CreateCommand(foodId), CancellationToken.None);

        await using var ctx = CreateContext();
        var act = () => CreateGetHandler(ctx, StrangerId)
            .Handle(new GetMealPlanForDateQuery(Date, UserId), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Check_WhenUserOwnsItem_CreatesFoodLogAndUpdatesDailyNutrition()
    {
        var foodId = await SeedBaseDataAsync();
        var itemId = await CreatePlanAndGetItemIdAsync(foodId);

        await using var ctx = CreateContext();
        var result = await CreateCheckHandler(ctx, UserId).Handle(new CheckMealPlanItemCommand(itemId), CancellationToken.None);

        result.CheckedItemCount.Should().Be(1);
        var foodLogs = await ctx.FoodLogs.ToListAsync();
        foodLogs.Should().ContainSingle();
        foodLogs[0].ComputedCalories.Should().Be(150);

        var dailyLog = await ctx.NutritionDailyLogs.SingleAsync(l => l.UserId == UserId && l.Date == Date);
        dailyLog.Calories.Should().Be(150);
        dailyLog.ProteinG.Should().Be(15m);
    }

    [Fact]
    public async Task Check_WhenCalledTwice_IsIdempotent()
    {
        var foodId = await SeedBaseDataAsync();
        var itemId = await CreatePlanAndGetItemIdAsync(foodId);

        await using var ctx = CreateContext();
        var handler = CreateCheckHandler(ctx, UserId);

        await handler.Handle(new CheckMealPlanItemCommand(itemId), CancellationToken.None);
        await handler.Handle(new CheckMealPlanItemCommand(itemId), CancellationToken.None);

        (await ctx.FoodLogs.CountAsync()).Should().Be(1);
        (await ctx.NutritionDailyLogs.SingleAsync()).Calories.Should().Be(150);
    }

    [Fact]
    public async Task Check_WhenCoachAttemptsClientItem_Throws()
    {
        var foodId = await SeedBaseDataAsync(withCoachRelationship: true);
        var itemId = await CreatePlanAndGetItemIdAsync(foodId);

        await using var ctx = CreateContext();
        var act = () => CreateCheckHandler(ctx, CoachId)
            .Handle(new CheckMealPlanItemCommand(itemId), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Uncheck_WhenItemWasChecked_RemovesLinkedFoodLogAndRecomputesDailyNutrition()
    {
        var foodId = await SeedBaseDataAsync();
        var itemId = await CreatePlanAndGetItemIdAsync(foodId);
        await using (var checkCtx = CreateContext())
            await CreateCheckHandler(checkCtx, UserId).Handle(new CheckMealPlanItemCommand(itemId), CancellationToken.None);

        await using var ctx = CreateContext();
        var result = await CreateUncheckHandler(ctx, UserId).Handle(new UncheckMealPlanItemCommand(itemId), CancellationToken.None);

        result.CheckedItemCount.Should().Be(0);
        (await ctx.FoodLogs.CountAsync()).Should().Be(0);
        var dailyLog = await ctx.NutritionDailyLogs.SingleAsync(l => l.UserId == UserId && l.Date == Date);
        dailyLog.Calories.Should().Be(0);
        dailyLog.ProteinG.Should().Be(0);
    }

    private UpsertMealPlanForDateHandler CreateUpsertHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, FoodLogService(ctx), new CoachClientRepository(ctx), new FakeCurrentUserService(currentUserId));

    private GetMealPlanForDateHandler CreateGetHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, new CoachClientRepository(ctx), new FakeCurrentUserService(currentUserId));

    private CheckMealPlanItemHandler CreateCheckHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, FoodLogService(ctx), new FakeCurrentUserService(currentUserId));

    private UncheckMealPlanItemHandler CreateUncheckHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, FoodLogService(ctx), new FakeCurrentUserService(currentUserId));

    private static FoodLogService FoodLogService(ApplicationDbContext ctx) =>
        new(ctx, new NutritionRepository(ctx));

    private UpsertMealPlanForDateCommand CreateCommand(Guid foodId, Guid? userId = null) =>
        new(
            Date,
            "Training day fuel",
            [
                new UpsertMealPlanMealRequest(
                    null,
                    "Breakfast",
                    1,
                    [
                        new UpsertMealPlanItemRequest(null, foodId, 1, 150m, null, null)
                    ])
            ],
            userId);

    private async Task<Guid> CreatePlanAndGetItemIdAsync(Guid foodId)
    {
        await using var ctx = CreateContext();
        var result = await CreateUpsertHandler(ctx, UserId).Handle(CreateCommand(foodId), CancellationToken.None);
        return result.Meals.Single().Items.Single().Id;
    }

    private async Task<Guid> SeedBaseDataAsync(bool withCoachRelationship = false)
    {
        await using var ctx = CreateContext();
        ctx.Users.AddRange(
            CreateUser(UserId, "Client"),
            CreateUser(CoachId, "Coach"),
            CreateUser(StrangerId, "Stranger"));

        var food = new FoodItem
        {
            NameVi = "Com ga",
            NameEn = "Chicken rice",
            CaloriesPer100g = 100m,
            ProteinPer100g = 10m,
            CarbsPer100g = 20m,
            FatPer100g = 3m
        };
        ctx.FoodItems.Add(food);

        if (withCoachRelationship)
        {
            ctx.CoachClientRelationships.Add(new CoachClientRelationship
            {
                CoachId = CoachId,
                ClientId = UserId,
                Status = RelationshipStatus.Active,
                StartDate = Date.AddDays(-7)
            });
        }

        await ctx.SaveChangesAsync();
        return food.Id;
    }

    private static ApplicationUser CreateUser(Guid id, string name) =>
        new()
        {
            Id = id,
            FirstName = name,
            LastName = "User",
            Email = $"{name.ToLowerInvariant()}@test.com",
            UserName = $"{name.ToLowerInvariant()}@test.com"
        };
}
