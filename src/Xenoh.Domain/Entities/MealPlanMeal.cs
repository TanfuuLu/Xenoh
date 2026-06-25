using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class MealPlanMeal : BaseEntity
{
    public Guid MealPlanDayId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public MealPlanDay MealPlanDay { get; set; } = null!;
    public ICollection<MealPlanItem> Items { get; set; } = [];
}
