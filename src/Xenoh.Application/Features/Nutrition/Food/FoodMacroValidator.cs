namespace Xenoh.Application.Features.Nutrition.Food;

/// <summary>
/// Guards the macro values that reach the FoodItems table.
///
/// The AI prompt asks the model for non-negative values and calorie/macro consistency, but a
/// prompt is a request, not a constraint: response_format only guarantees the JSON shape, never
/// the numbers inside it. Everything written to FoodItems passes through here instead.
/// </summary>
public static class FoodMacroValidator
{
    /// <summary>Pure fat is 9 kcal/g, so nothing edible exceeds this per 100g.</summary>
    private const decimal MaxCaloriesPer100g = 900m;

    /// <summary>A single macro cannot outweigh the 100g it is measured in.</summary>
    private const decimal MaxGramsPer100g = 100m;

    /// <summary>
    /// Protein + carbs + fat must fit inside 100g. The slack absorbs rounding in published
    /// composition tables, which report each macro independently.
    /// </summary>
    private const decimal MaxCombinedGramsPer100g = 101m;

    /// <summary>
    /// Atwater factors. Real foods drift a few percent from these because fiber and polyols are
    /// counted as carbohydrate but yield less energy, so the tolerance is deliberately loose.
    /// </summary>
    private const decimal AtwaterTolerance = 0.25m;

    /// <summary>
    /// Physical limits only. Used for values a human typed on purpose, where silently rewriting
    /// their input would be worse than trusting it.
    /// </summary>
    public static void ValidatePhysical(
        string displayName,
        decimal calories,
        decimal protein,
        decimal carbs,
        decimal fat)
    {
        if (calories < 0 || protein < 0 || carbs < 0 || fat < 0)
            throw new InvalidOperationException(
                $"Nutrition values for \"{displayName}\" cannot be negative.");

        if (protein > MaxGramsPer100g || carbs > MaxGramsPer100g || fat > MaxGramsPer100g)
            throw new InvalidOperationException(
                $"Nutrition values for \"{displayName}\" are out of range: no single macronutrient can exceed {MaxGramsPer100g:0}g per 100g.");

        if (protein + carbs + fat > MaxCombinedGramsPer100g)
            throw new InvalidOperationException(
                $"Nutrition values for \"{displayName}\" are out of range: protein, carbs and fat add up to more than 100g per 100g.");

        if (calories > MaxCaloriesPer100g)
            throw new InvalidOperationException(
                $"Nutrition values for \"{displayName}\" are out of range: no food exceeds {MaxCaloriesPer100g:0} kcal per 100g.");
    }

    /// <summary>
    /// Physical limits plus a calorie cross-check, for values the model produced.
    /// </summary>
    /// <returns>
    /// The calorie figure to store. When the model's calories contradict its own macros beyond
    /// the tolerance, the macros win and calories are recomputed from them: the macros are the
    /// values the athlete's protein and carb targets are tracked against, so a self-consistent
    /// row matters more than preserving a number the model already contradicted.
    /// </returns>
    public static decimal ValidateAiResult(
        string displayName,
        decimal calories,
        decimal protein,
        decimal carbs,
        decimal fat)
    {
        ValidatePhysical(displayName, calories, protein, carbs, fat);

        var fromMacros = (4m * protein) + (4m * carbs) + (9m * fat);

        // A genuinely zero-calorie item (salt, black coffee, most spices) is consistent by
        // definition and must not be dragged off zero by the deviation maths below.
        if (calories == 0m && fromMacros == 0m)
            return 0m;

        var deviation = Math.Abs(calories - fromMacros) / Math.Max(calories, 1m);
        return deviation > AtwaterTolerance
            ? Math.Round(fromMacros, 2)
            : calories;
    }
}
