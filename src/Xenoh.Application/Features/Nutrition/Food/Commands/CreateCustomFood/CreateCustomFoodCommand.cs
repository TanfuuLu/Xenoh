using Mediator;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.CreateCustomFood;

public sealed record CreateCustomFoodCommand(
    string NameVi,
    string NameEn,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    string? DefaultServingLabel,
    decimal? DefaultServingGrams
) : IRequest<FoodItemResponse>;
