using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class MealPlanDay : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<MealPlanMeal> Meals { get; set; } = [];
}
