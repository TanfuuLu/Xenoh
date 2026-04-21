using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class DailyWorkout : BaseEntity
{
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsCompleted { get; set; }

    public Guid WeeklyWorkoutId { get; set; }
    public WeeklyWorkout WeeklyWorkout { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = [];
}
