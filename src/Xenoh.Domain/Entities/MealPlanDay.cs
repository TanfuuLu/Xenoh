using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class MealPlanDay : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Who last wrote this day — the coach when they plan for a client, otherwise the
    /// client themselves. Null on rows that predate authorship tracking, which are
    /// treated as client-owned. Lets a disconnect remove the coach's planning without
    /// touching what the client wrote for themselves.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<MealPlanMeal> Meals { get; set; } = [];
}
