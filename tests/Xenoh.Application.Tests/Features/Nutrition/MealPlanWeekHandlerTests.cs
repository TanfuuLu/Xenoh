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

public sealed class MealPlanWeekHandlerTests : HandlerTestBase
{
    private readonly Guid CoachId = Guid.NewGuid();
    private readonly Guid StrangerId = Guid.NewGuid();
    private readonly DateOnly StartDate = new(2026, 6, 22);

    [Fact]
    public async Task ApplyTemplate_WhenRangeIsValid_CreatesIndependentDailyCopies()
    {
        var foodId = await SeedBaseDataAsync();

        await using var ctx = CreateContext();
        var result = await CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(6)), CancellationToken.None);

        result.StartDate.Should().Be(StartDate);
        result.EndDate.Should().Be(StartDate.AddDays(6));
        result.AffectedDayCount.Should().Be(7);
        (await ctx.MealPlanDays.CountAsync()).Should().Be(7);
        (await ctx.MealPlanMeals.CountAsync()).Should().Be(7);
        (await ctx.MealPlanItems.CountAsync()).Should().Be(7);
        (await ctx.MealPlanItems.Select(item => item.Id).Distinct().CountAsync()).Should().Be(7);
        (await ctx.MealPlanItems.SumAsync(item => item.PlannedCalories)).Should().Be(1050);
    }

    [Fact]
    public async Task ApplyTemplate_WhenRangeIsOneDay_CreatesOnePlan()
    {
        var foodId = await SeedBaseDataAsync();

        await using var ctx = CreateContext();
        var result = await CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate), CancellationToken.None);

        result.AffectedDayCount.Should().Be(1);
        (await ctx.MealPlanDays.SingleAsync()).Date.Should().Be(StartDate);
    }

    [Fact]
    public async Task ApplyTemplate_WhenRangeIsReversed_RejectsWithoutSaving()
    {
        var foodId = await SeedBaseDataAsync();
        await using var ctx = CreateContext();

        var act = () => CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(-1)), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*end date*");
        (await ctx.MealPlanDays.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ApplyTemplate_WhenRangeExceedsNinetyDays_RejectsWithoutSaving()
    {
        var foodId = await SeedBaseDataAsync();
        await using var ctx = CreateContext();

        var act = () => CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(90)), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*90 days*");
        (await ctx.MealPlanDays.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ApplyTemplate_WhenReplacingUncheckedPlans_ReplacesEveryDate()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
        {
            var dailyHandler = CreateDailyHandler(setup, UserId);
            await dailyHandler.Handle(CreateDailyCommand(foodId, StartDate, 100m), CancellationToken.None);
            await dailyHandler.Handle(CreateDailyCommand(foodId, StartDate.AddDays(1), 100m), CancellationToken.None);
        }

        await using var ctx = CreateContext();
        await CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(1), 200m), CancellationToken.None);

        (await ctx.MealPlanDays.CountAsync()).Should().Be(2);
        (await ctx.MealPlanItems.CountAsync()).Should().Be(2);
        (await ctx.MealPlanItems.ToListAsync()).Should().OnlyContain(item => item.Grams == 200m);
    }

    [Fact]
    public async Task ApplyTemplate_WhenAnyExistingItemIsChecked_RejectsEntireRange()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
        {
            await CreateApplyHandler(setup, UserId)
                .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(1)), CancellationToken.None);
            var checkedItem = await setup.MealPlanItems.FirstAsync();
            checkedItem.IsChecked = true;
            await setup.SaveChangesAsync();
        }

        await using var ctx = CreateContext();
        var act = () => CreateApplyHandler(ctx, UserId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(1), 200m), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Uncheck meal plan items*");
        (await ctx.MealPlanItems.CountAsync()).Should().Be(2);
        (await ctx.MealPlanItems.ToListAsync()).Should().OnlyContain(item => item.Grams == 150m);
    }

    [Fact]
    public async Task ApplyTemplate_WhenFoodIsInvalid_DoesNotPartiallyReplaceRange()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
        {
            await CreateApplyHandler(setup, UserId)
                .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(1)), CancellationToken.None);
        }

        var invalid = CreateApplyCommand(Guid.NewGuid(), StartDate, StartDate.AddDays(1), 200m);
        await using (var ctx = CreateContext())
        {
            var act = () => CreateApplyHandler(ctx, UserId).Handle(invalid, CancellationToken.None).AsTask();
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var verify = CreateContext();
        (await verify.MealPlanItems.CountAsync()).Should().Be(2);
        (await verify.MealPlanItems.ToListAsync()).Should().OnlyContain(item => item.Grams == 150m);
    }

    [Fact]
    public async Task ApplyTemplate_AfterOneGeneratedDayIsEdited_OtherDaysRemainUnchanged()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
        {
            await CreateApplyHandler(setup, UserId)
                .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(1)), CancellationToken.None);
        }

        await using (var edit = CreateContext())
        {
            await CreateDailyHandler(edit, UserId)
                .Handle(CreateDailyCommand(foodId, StartDate, 250m), CancellationToken.None);
        }

        await using var verify = CreateContext();
        var days = await verify.MealPlanDays
            .Include(day => day.Meals)
            .ThenInclude(meal => meal.Items)
            .OrderBy(day => day.Date)
            .ToListAsync();
        days[0].Meals.Single().Items.Single().Grams.Should().Be(250m);
        days[1].Meals.Single().Items.Single().Grams.Should().Be(150m);
    }

    [Fact]
    public async Task ApplyTemplate_WhenActiveCoachEditsClient_CreatesClientPlans()
    {
        var foodId = await SeedBaseDataAsync(withCoachRelationship: true);

        await using var ctx = CreateContext();
        var result = await CreateApplyHandler(ctx, CoachId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate.AddDays(2), userId: UserId), CancellationToken.None);

        result.AffectedDayCount.Should().Be(3);
        (await ctx.MealPlanDays.ToListAsync()).Should().OnlyContain(day => day.UserId == UserId);
    }

    [Fact]
    public async Task ApplyTemplate_WhenUnrelatedUserEditsClient_Throws()
    {
        var foodId = await SeedBaseDataAsync();
        await using var ctx = CreateContext();

        var act = () => CreateApplyHandler(ctx, StrangerId)
            .Handle(CreateApplyCommand(foodId, StartDate, StartDate, userId: UserId), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Get_WhenOnlyOneDayExists_ReturnsSevenOrderedDaysIncludingEmptyDays()
    {
        var foodId = await SeedBaseDataAsync();
        await using (var setup = CreateContext())
            await CreateDailyHandler(setup, UserId)
                .Handle(CreateDailyCommand(foodId, StartDate), CancellationToken.None);

        await using var ctx = CreateContext();
        var result = await CreateGetHandler(ctx, UserId)
            .Handle(new GetMealPlanWeekQuery(StartDate), CancellationToken.None);

        result.Days.Should().HaveCount(7);
        result.Days[0].TotalItemCount.Should().Be(1);
        result.Days.Skip(1).Should().OnlyContain(day => day.Id == null && day.TotalItemCount == 0);
        result.TotalItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_WhenStartDateIsNotMonday_RejectsRequest()
    {
        await SeedBaseDataAsync();
        await using var ctx = CreateContext();

        var act = () => CreateGetHandler(ctx, UserId)
            .Handle(new GetMealPlanWeekQuery(StartDate.AddDays(1)), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Monday*");
    }

    private ApplyMealPlanTemplateHandler CreateApplyHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, FoodLogService(ctx), new CoachClientRepository(ctx), new FakeCurrentUserService(currentUserId));

    private GetMealPlanWeekHandler CreateGetHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, new CoachClientRepository(ctx), new FakeCurrentUserService(currentUserId));

    private UpsertMealPlanForDateHandler CreateDailyHandler(ApplicationDbContext ctx, Guid currentUserId) =>
        new(ctx, FoodLogService(ctx), new CoachClientRepository(ctx), new FakeCurrentUserService(currentUserId));

    private static FoodLogService FoodLogService(ApplicationDbContext ctx) =>
        new(ctx, new NutritionRepository(ctx));

    private static ApplyMealPlanTemplateCommand CreateApplyCommand(
        Guid foodId,
        DateOnly startDate,
        DateOnly endDate,
        decimal grams = 150m,
        Guid? userId = null) =>
        new(
            startDate,
            endDate,
            "Fuel",
            [
                new UpsertMealPlanMealRequest(
                    null,
                    "Breakfast",
                    0,
                    [new UpsertMealPlanItemRequest(null, foodId, 0, grams, null, null)])
            ],
            userId);

    private static UpsertMealPlanForDateCommand CreateDailyCommand(
        Guid foodId,
        DateOnly date,
        decimal grams = 150m) =>
        new(
            date,
            "Fuel",
            [
                new UpsertMealPlanMealRequest(
                    null,
                    "Breakfast",
                    0,
                    [new UpsertMealPlanItemRequest(null, foodId, 0, grams, null, null)])
            ]);

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
        food.Servings.Add(new FoodServing
        {
            FoodItemId = food.Id,
            LabelVi = "Phan",
            LabelEn = "Serving",
            Grams = 100m
        });
        ctx.FoodItems.Add(food);

        if (withCoachRelationship)
        {
            ctx.CoachClientRelationships.Add(new CoachClientRelationship
            {
                CoachId = CoachId,
                ClientId = UserId,
                Status = RelationshipStatus.Active,
                StartDate = StartDate.AddDays(-7)
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
