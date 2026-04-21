using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class WeeklyWorkout : BaseEntity
{
    public int WeekNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public ICollection<DailyWorkout> DailyWorkouts { get; set; } = [];
}
