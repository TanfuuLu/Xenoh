using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;

public sealed class SearchFoodHandler(
    IApplicationDbContext db,
    ICurrentUserService? currentUser = null,
    IApplicationCache? cache = null)
    : IRequestHandler<SearchFoodQuery, List<FoodItemResponse>>
{
    public ValueTask<List<FoodItemResponse>> Handle(SearchFoodQuery request, CancellationToken cancellationToken)
    {
        var q = request.Query.Trim().ToLower();
        var userId = currentUser?.UserId ?? Guid.Empty;
        return new ValueTask<List<FoodItemResponse>>(cache is null
            ? SearchAsync(q, userId, cancellationToken)
            : cache.GetOrCreateAsync(
                CacheTags.Foods,
                $"user:{userId:N}:query:{q}",
                TimeSpan.FromMinutes(5),
                ct => SearchAsync(q, userId, ct),
                cancellationToken));
    }

    private Task<List<FoodItemResponse>> SearchAsync(string q, Guid userId, CancellationToken cancellationToken) =>
        db.FoodItems
            .AsNoTracking()
            // Only the curated catalogue is shared. AI-resolved and user-created rows belong to
            // whoever caused them, so one person's custom food never surfaces in another's search
            // and an AI estimate cannot become the global answer for a name.
            .Where(f => f.Source == FoodItemSource.Seed || f.CreatedByUserId == userId)
            .Where(f =>
                f.NameVi.ToLower().Contains(q) ||
                f.NameEn.ToLower().Contains(q))
            .OrderBy(f => f.NameVi)
            .Take(20)
            .Select(f => new FoodItemResponse(
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
            ))
            .ToListAsync(cancellationToken);
}
