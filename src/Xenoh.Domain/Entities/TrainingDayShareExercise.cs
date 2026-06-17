using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class TrainingDayShareExercise : BaseEntity
{
    public Guid TrainingDayShareId { get; set; }
    public TrainingDayShare TrainingDayShare { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public ExerciseKind ExerciseKind { get; set; } = ExerciseKind.Strength;
    public int SortOrder { get; set; }
    public bool IsSkipped { get; set; }
    public bool IsPersonalRecord { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Notes { get; set; }

    public ICollection<TrainingDayShareSet> Sets { get; set; } = [];
}
