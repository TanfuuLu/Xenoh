using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class MealPlanItem : BaseEntity
{
    public Guid MealPlanMealId { get; set; }
    public Guid FoodItemId { get; set; }
    public int SortOrder { get; set; }
    public decimal Grams { get; set; }
    public string? ServingLabelVi { get; set; }
    public string? ServingLabelEn { get; set; }
    public decimal? ServingCount { get; set; }
    public int PlannedCalories { get; set; }
    public decimal PlannedProteinG { get; set; }
    public decimal PlannedCarbsG { get; set; }
    public decimal PlannedFatG { get; set; }
    public bool IsChecked { get; set; }
    public DateTime? CheckedAt { get; set; }
    public Guid? CheckedByUserId { get; set; }
    public Guid? FoodLogId { get; set; }

    public MealPlanMeal MealPlanMeal { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
    public ApplicationUser? CheckedByUser { get; set; }
    public FoodLog? FoodLog { get; set; }
}
