using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.CreateCustomFood;

public sealed class CreateCustomFoodHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<CreateCustomFoodCommand, FoodItemResponse>
{
    public async ValueTask<FoodItemResponse> Handle(CreateCustomFoodCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Physical limits only. These numbers were typed deliberately, so an impossible value is
        // rejected and reported rather than silently rewritten the way an AI estimate would be.
        FoodMacroValidator.ValidatePhysical(
            request.NameVi.Trim(),
            request.CaloriesPer100g,
            request.ProteinPer100g,
            request.CarbsPer100g,
            request.FatPer100g);

        var food = new FoodItem
        {
            NameVi = request.NameVi.Trim(),
            NameEn = request.NameEn.Trim(),
            CaloriesPer100g = request.CaloriesPer100g,
            ProteinPer100g = request.ProteinPer100g,
            CarbsPer100g = request.CarbsPer100g,
            FatPer100g = request.FatPer100g,
            Source = FoodItemSource.UserCustom,
            CreatedByUserId = userId,
            IsVerified = false
        };

        await db.FoodItems.AddAsync(food, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.DefaultServingLabel) && request.DefaultServingGrams is not null)
        {
            await db.FoodServings.AddAsync(new FoodServing
            {
                FoodItemId = food.Id,
                LabelVi = request.DefaultServingLabel,
                Grams = request.DefaultServingGrams.Value
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.FoodItems
            .AsNoTracking()
            .Where(f => f.Id == food.Id)
            .Include(f => f.Servings)
            .FirstAsync(cancellationToken);

        return new FoodItemResponse(
            saved.Id,
            saved.NameVi,
            saved.NameEn,
            saved.CaloriesPer100g,
            saved.ProteinPer100g,
            saved.CarbsPer100g,
            saved.FatPer100g,
            saved.Servings.Select(s => new FoodServingResponse(s.Id, s.LabelVi, s.LabelEn, s.Grams)).ToList(),
            saved.Source,
            saved.IsVerified
        );
    }
}
