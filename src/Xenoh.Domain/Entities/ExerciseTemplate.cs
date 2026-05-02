using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class ExerciseTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OwnerId { get; set; }
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public List<MuscleGroup> SecondaryMuscleGroups { get; set; } = [];
    public ExerciseKind ExerciseKind { get; set; } = ExerciseKind.Strength;
    public decimal EstimatedMet { get; set; } = 5.0m;
    public bool IsCompetitionLift { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public CompetitionLiftType? CompetitionLiftType { get; set; }
    public string? ImageUrl { get; set; }
    public ApplicationUser? Owner { get; set; }
}
