using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class FoodServing : BaseEntity
{
    public Guid FoodItemId { get; set; }
    public string LabelVi { get; set; } = string.Empty;
    public string? LabelEn { get; set; }
    public decimal Grams { get; set; }

    public FoodItem FoodItem { get; set; } = null!;
}
