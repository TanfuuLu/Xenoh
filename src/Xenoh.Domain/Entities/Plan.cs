using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PlanType PlanType { get; set; }

    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;

    // Only set if PlanType == Coach
    public Guid? CreatedByCoachId { get; set; }
    public ApplicationUser? CreatedByCoach { get; set; }

    public bool IsActive { get; set; } = false;

    public ICollection<WeeklyWorkout> WeeklyWorkouts { get; set; } = [];
    public ICollection<PlanComment> Comments { get; set; } = [];
}
