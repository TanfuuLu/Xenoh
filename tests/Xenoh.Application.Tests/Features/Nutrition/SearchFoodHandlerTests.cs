using FluentAssertions;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Nutrition;

public sealed class SearchFoodHandlerTests : HandlerTestBase
{
    private readonly Guid _otherUserId = Guid.NewGuid();

    private async Task SeedAsync()
    {
        await using var db = CreateContext();
        db.FoodItems.AddRange(
            new FoodItem
            {
                NameVi = "Ức gà",
                NameEn = "Chicken breast",
                CaloriesPer100g = 165m,
                ProteinPer100g = 31m,
                CarbsPer100g = 0m,
                FatPer100g = 3.6m,
                Source = FoodItemSource.Seed,
                IsVerified = true
            },
            new FoodItem
            {
                NameVi = "Ức gà mẹ nấu",
                NameEn = "Chicken breast home style",
                CaloriesPer100g = 220m,
                ProteinPer100g = 25m,
                CarbsPer100g = 2m,
                FatPer100g = 12m,
                Source = FoodItemSource.UserCustom,
                CreatedByUserId = _otherUserId,
                IsVerified = false
            },
            new FoodItem
            {
                NameVi = "Ức gà AI",
                NameEn = "Chicken breast ai",
                CaloriesPer100g = 180m,
                ProteinPer100g = 28m,
                CarbsPer100g = 1m,
                FatPer100g = 6m,
                Source = FoodItemSource.Ai,
                CreatedByUserId = UserId,
                IsVerified = false
            });
        await db.SaveChangesAsync();
    }

    private async Task<List<FoodItemResponse>> SearchAsync(Guid asUser)
    {
        await using var db = CreateContext();
        var handler = new SearchFoodHandler(db, new FakeCurrentUserService(asUser));
        return await handler.Handle(new SearchFoodQuery("ức gà"), CancellationToken.None);
    }

    [Fact]
    public async Task Search_DoesNotLeakAnotherUsersCustomFood()
    {
        await SeedAsync();

        var results = await SearchAsync(UserId);

        results.Should().NotContain(f => f.NameEn == "Chicken breast home style");
    }

    [Fact]
    public async Task Search_ReturnsSeedCatalogueToEveryone()
    {
        await SeedAsync();

        var results = await SearchAsync(UserId);

        results.Should().Contain(f => f.NameEn == "Chicken breast");
    }

    [Fact]
    public async Task Search_ReturnsCallersOwnAiResolvedFood()
    {
        await SeedAsync();

        var results = await SearchAsync(UserId);

        results.Should().Contain(f => f.NameEn == "Chicken breast ai");
    }

    [Fact]
    public async Task Search_DoesNotShowOneUsersAiEstimateToAnother()
    {
        await SeedAsync();

        var results = await SearchAsync(_otherUserId);

        results.Should().NotContain(f => f.NameEn == "Chicken breast ai");
        // ...but that user still sees their own custom entry and the shared catalogue.
        results.Should().Contain(f => f.NameEn == "Chicken breast home style");
        results.Should().Contain(f => f.NameEn == "Chicken breast");
    }

    [Fact]
    public async Task Search_ExposesProvenanceSoTheUiCanFlagEstimates()
    {
        await SeedAsync();

        var results = await SearchAsync(UserId);

        results.Single(f => f.NameEn == "Chicken breast")
            .Should().Match<FoodItemResponse>(f => f.Source == FoodItemSource.Seed && f.IsVerified);
        results.Single(f => f.NameEn == "Chicken breast ai")
            .Should().Match<FoodItemResponse>(f => f.Source == FoodItemSource.Ai && !f.IsVerified);
    }

    [Fact]
    public async Task Search_WithoutAuthenticatedUser_ReturnsOnlySeedCatalogue()
    {
        await SeedAsync();

        await using var db = CreateContext();
        var handler = new SearchFoodHandler(db);
        var results = await handler.Handle(new SearchFoodQuery("ức gà"), CancellationToken.None);

        results.Should().ContainSingle().Which.Source.Should().Be(FoodItemSource.Seed);
    }
}
