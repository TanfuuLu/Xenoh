using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class NutritionProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Moderate;
    public NutritionGoal Goal { get; set; } = NutritionGoal.Maintain;
    public decimal? TargetWeightKg { get; set; }
    public int? CustomCalorieTarget { get; set; }
    public decimal? ProteinPerKg { get; set; }
    public decimal? FatPerKg { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
