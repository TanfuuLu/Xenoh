using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class TrainingDayShare : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid SourceDailyWorkoutId { get; set; }
    public DateOnly WorkoutDate { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DayStatus DayStatus { get; set; }
    public int ExerciseCount { get; set; }
    public int CompletedSets { get; set; }
    public decimal TotalVolume { get; set; }
    public int TotalDurationSeconds { get; set; }
    public decimal? AverageRpe { get; set; }
    public bool HasPersonalRecord { get; set; }
    public string? Caption { get; set; }

    public ICollection<TrainingDayShareExercise> Exercises { get; set; } = [];
    public ICollection<TrainingDayShareLove> Loves { get; set; } = [];
}
