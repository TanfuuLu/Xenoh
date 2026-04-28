using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }
    public ApplicationUser Recipient { get; set; } = null!;

    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
