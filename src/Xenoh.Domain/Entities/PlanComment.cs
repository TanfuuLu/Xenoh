using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class PlanComment : BaseEntity
{
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
}
