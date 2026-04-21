using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class Exercise : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public List<MuscleGroup> SecondaryMuscleGroups { get; set; } = [];

    public Guid ExerciseTemplateId { get; set; }
    public ExerciseTemplate ExerciseTemplate { get; set; } = null!;

    public int PlannedSets { get; set; }
    public int PlannedReps { get; set; }
    public decimal? PlannedWeight { get; set; }

    public bool IsCompleted { get; set; }

    public string? Notes { get; set; }

    public Guid DailyWorkoutId { get; set; }
    public DailyWorkout DailyWorkout { get; set; } = null!;

    public ICollection<ExerciseSet> Sets { get; set; } = [];
}
