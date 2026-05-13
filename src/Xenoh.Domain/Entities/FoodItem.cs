using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class FoodItem : BaseEntity
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal CaloriesPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }
    public decimal FatPer100g { get; set; }
    public FoodItemSource Source { get; set; } = FoodItemSource.Seed;
    public Guid? CreatedByUserId { get; set; }
    public bool IsVerified { get; set; } = true;

    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<FoodServing> Servings { get; set; } = [];
    public ICollection<FoodLog> FoodLogs { get; set; } = [];
}
