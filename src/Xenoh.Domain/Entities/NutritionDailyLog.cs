using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class NutritionDailyLog : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public int Calories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
