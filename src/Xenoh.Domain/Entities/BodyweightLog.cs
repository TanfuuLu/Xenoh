using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class BodyweightLog : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Weight { get; set; }  // kg
    public DateOnly Date { get; set; }
}
