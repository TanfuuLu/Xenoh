using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class WeeklyWorkoutComment : BaseEntity
{
    public Guid WeeklyWorkoutId { get; set; }
    public WeeklyWorkout WeeklyWorkout { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
}
