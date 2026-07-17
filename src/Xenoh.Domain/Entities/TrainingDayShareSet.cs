using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class TrainingDayShareSet : BaseEntity
{
    public Guid TrainingDayShareExerciseId { get; set; }
    public TrainingDayShareExercise TrainingDayShareExercise { get; set; } = null!;

    public int SetNumber { get; set; }
    public int PlannedReps { get; set; }
    public decimal? PlannedWeight { get; set; }
    public int? ActualReps { get; set; }
    public decimal? ActualWeight { get; set; }
    public decimal? Rpe { get; set; }
    public bool IsCompleted { get; set; }
}
