using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class UserBlock : BaseEntity
{
    public Guid BlockerId { get; set; }
    public ApplicationUser Blocker { get; set; } = null!;

    public Guid BlockedId { get; set; }
    public ApplicationUser Blocked { get; set; } = null!;

    public string? Reason { get; set; }
}
