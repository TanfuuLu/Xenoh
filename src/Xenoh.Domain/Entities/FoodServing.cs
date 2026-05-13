using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class FoodServing : BaseEntity
{
    public Guid FoodItemId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Grams { get; set; }

    public FoodItem FoodItem { get; set; } = null!;
}
