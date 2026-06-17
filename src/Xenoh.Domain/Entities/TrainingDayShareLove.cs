using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class TrainingDayShareLove : BaseEntity
{
    public Guid TrainingDayShareId { get; set; }
    public TrainingDayShare TrainingDayShare { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
