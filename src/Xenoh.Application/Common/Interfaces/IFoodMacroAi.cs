namespace Xenoh.Application.Common.Interfaces;

public sealed record FoodMacroAiRequest(string FoodName, string Language = "vi");

public sealed record FoodMacroAiResult(
    string NameVi,
    string NameEn,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    string? DefaultServingLabel,
    decimal? DefaultServingGrams
);

public interface IFoodMacroAi
{
    Task<FoodMacroAiResult> ResolveAsync(FoodMacroAiRequest request, CancellationToken cancellationToken);
}
