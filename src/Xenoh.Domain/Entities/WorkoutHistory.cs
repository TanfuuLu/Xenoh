using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class WorkoutHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
}
