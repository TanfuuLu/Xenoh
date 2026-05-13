using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.ResolveFood;

public sealed class ResolveFoodHandler(
    IApplicationDbContext db,
    IFoodMacroAi foodMacroAi
) : IRequestHandler<ResolveFoodQuery, FoodItemResponse>
{
    public async ValueTask<FoodItemResponse> Handle(ResolveFoodQuery request, CancellationToken cancellationToken)
    {
        // Check if we already have it in DB (case-insensitive)
        var nameLower = request.Name.Trim().ToLower();
        var existing = await db.FoodItems
            .AsNoTracking()
            .Where(f =>
                f.NameVi.ToLower() == nameLower ||
                f.NameEn.ToLower() == nameLower)
            .Include(f => f.Servings)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return ToResponse(existing);

        // Call AI to resolve macros
        var result = await foodMacroAi.ResolveAsync(new(request.Name), cancellationToken);

        var food = new FoodItem
        {
            NameVi = result.NameVi,
            NameEn = result.NameEn,
            CaloriesPer100g = result.CaloriesPer100g,
            ProteinPer100g = result.ProteinPer100g,
            CarbsPer100g = result.CarbsPer100g,
            FatPer100g = result.FatPer100g,
            Source = FoodItemSource.Ai,
            IsVerified = false
        };

        await db.FoodItems.AddAsync(food, cancellationToken);

        if (result.DefaultServingLabel is not null && result.DefaultServingGrams is not null)
        {
            await db.FoodServings.AddAsync(new FoodServing
            {
                FoodItemId = food.Id,
                Label = result.DefaultServingLabel,
                Grams = result.DefaultServingGrams.Value
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Reload with servings
        var saved = await db.FoodItems
            .AsNoTracking()
            .Where(f => f.Id == food.Id)
            .Include(f => f.Servings)
            .FirstAsync(cancellationToken);

        return ToResponse(saved);
    }

    private static FoodItemResponse ToResponse(FoodItem f) =>
        new(
            f.Id,
            f.NameVi,
            f.NameEn,
            f.CaloriesPer100g,
            f.ProteinPer100g,
            f.CarbsPer100g,
            f.FatPer100g,
            f.Servings.Select(s => new FoodServingResponse(s.Id, s.Label, s.Grams)).ToList()
        );
}
