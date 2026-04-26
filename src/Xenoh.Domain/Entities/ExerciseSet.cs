using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class ExerciseSet : BaseEntity
{
    public int SetNumber { get; set; }
    public int PlannedReps { get; set; }
    public decimal? PlannedWeight { get; set; }

    public int? ActualReps { get; set; }
    public decimal? ActualWeight { get; set; }

    public decimal? Rpe { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
}
