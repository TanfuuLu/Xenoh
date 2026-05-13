using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class FoodLog : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid FoodItemId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Grams { get; set; }
    public string? ServingLabel { get; set; }
    public decimal? ServingCount { get; set; }
    public int ComputedCalories { get; set; }
    public decimal ComputedProteinG { get; set; }
    public decimal ComputedCarbsG { get; set; }
    public decimal ComputedFatG { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
