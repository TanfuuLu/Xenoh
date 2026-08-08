using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.ResolveFood;

public sealed class ResolveFoodHandler(
    IApplicationDbContext db,
    IFoodMacroAi foodMacroAi,
    ICurrentUserService? currentUser = null,
    IApplicationCache? cache = null,
    IDistributedLock? distributedLock = null
) : IRequestHandler<ResolveFoodQuery, FoodItemResponse>
{
    public ValueTask<FoodItemResponse> Handle(ResolveFoodQuery request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var userId = currentUser?.UserId ?? Guid.Empty;
        return new(cache is null
            ? ResolveAsync(request, normalizedName, userId, cancellationToken)
            : cache.GetOrCreateAsync(
                CacheTags.Foods,
                $"user:{userId:N}:resolve:{normalizedName}",
                TimeSpan.FromMinutes(5),
                ct => ResolveAsync(request, normalizedName, userId, ct),
                cancellationToken));
    }

    private async Task<FoodItemResponse> ResolveAsync(
        ResolveFoodQuery request,
        string nameLower,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Check if we already have it in DB (case-insensitive), within what this user can see.
        var existing = await VisibleFoods(userId)
            .Where(f =>
                f.NameVi.ToLower() == nameLower ||
                f.NameEn.ToLower() == nameLower)
            .Include(f => f.Servings)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return ToResponse(existing);

        // The lock suppresses duplicate external AI calls from concurrent API nodes. It is keyed
        // per user because the row it guards is now per user: two people resolving the same name
        // each need their own, so they must not serialise behind one another.
        await using var lease = distributedLock is null
            ? null
            : await distributedLock.TryAcquireAsync($"food:{userId:N}:{nameLower}", TimeSpan.FromSeconds(30), cancellationToken);

        if (distributedLock is not null && lease is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            var concurrentResult = await VisibleFoods(userId)
                .Where(f => f.NameVi.ToLower() == nameLower || f.NameEn.ToLower() == nameLower)
                .Include(f => f.Servings)
                .FirstOrDefaultAsync(cancellationToken);
            if (concurrentResult is not null)
                return ToResponse(concurrentResult);
        }

        // Call AI to resolve macros.
        var result = await foodMacroAi.ResolveAsync(new(request.Name), cancellationToken);

        // The model is not trusted with the numbers it just produced. Impossible values are
        // rejected outright; calories that contradict the macros are recomputed from them.
        var calories = FoodMacroValidator.ValidateAiResult(
            request.Name.Trim(),
            result.CaloriesPer100g,
            result.ProteinPer100g,
            result.CarbsPer100g,
            result.FatPer100g);

        var food = new FoodItem
        {
            NameVi = result.NameVi,
            NameEn = result.NameEn,
            CaloriesPer100g = calories,
            ProteinPer100g = result.ProteinPer100g,
            CarbsPer100g = result.CarbsPer100g,
            FatPer100g = result.FatPer100g,
            Source = FoodItemSource.Ai,
            CreatedByUserId = userId == Guid.Empty ? null : userId,
            IsVerified = false
        };

        await db.FoodItems.AddAsync(food, cancellationToken);

        if (result.DefaultServingLabel is not null && result.DefaultServingGrams is not null)
        {
            await db.FoodServings.AddAsync(new FoodServing
            {
                FoodItemId = food.Id,
                LabelVi = result.DefaultServingLabel,
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

    /// <summary>
    /// The curated catalogue plus this user's own rows. Mirrors the filter in SearchFoodHandler:
    /// resolve must not hand back a row that search would refuse to show.
    /// </summary>
    private IQueryable<FoodItem> VisibleFoods(Guid userId) =>
        db.FoodItems
            .AsNoTracking()
            .Where(f => f.Source == FoodItemSource.Seed || f.CreatedByUserId == userId);

    private static FoodItemResponse ToResponse(FoodItem f) =>
        new(
            f.Id,
            f.NameVi,
            f.NameEn,
            f.CaloriesPer100g,
            f.ProteinPer100g,
            f.CarbsPer100g,
            f.FatPer100g,
            f.Servings.Select(s => new FoodServingResponse(s.Id, s.LabelVi, s.LabelEn, s.Grams)).ToList(),
            f.Source,
            f.IsVerified
        );
}
