using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class DailyWorkout : BaseEntity
{
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsCompleted { get; set; }
    public DayStatus Status { get; set; } = DayStatus.Normal;

    public Guid WeeklyWorkoutId { get; set; }
    public WeeklyWorkout WeeklyWorkout { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = [];
}
