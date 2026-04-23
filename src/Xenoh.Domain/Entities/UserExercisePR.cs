using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class UserExercisePR : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ExerciseTemplateId { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public DateTime AchievedAt { get; set; }
}
