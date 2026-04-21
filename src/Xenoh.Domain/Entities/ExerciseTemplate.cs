using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class ExerciseTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public List<MuscleGroup> SecondaryMuscleGroups { get; set; } = [];
}
