using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;

public sealed class SearchFoodHandler(IApplicationDbContext db)
    : IRequestHandler<SearchFoodQuery, List<FoodItemResponse>>
{
    public async ValueTask<List<FoodItemResponse>> Handle(SearchFoodQuery request, CancellationToken cancellationToken)
    {
        var q = request.Query.Trim().ToLower();

        return await db.FoodItems
            .AsNoTracking()
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
                f.Servings.Select(s => new FoodServingResponse(s.Id, s.Label, s.Grams)).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}
