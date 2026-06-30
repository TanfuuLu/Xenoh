using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class Message : BaseEntity
{
    public Guid RelationshipId { get; set; }
    public CoachClientRelationship Relationship { get; set; } = null!;

    public Guid SenderId { get; set; }
    public ApplicationUser Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public MessageKind Kind { get; set; } = MessageKind.User;
    public bool IsRead { get; set; } = false;

    public ICollection<ChatMessageAttachment> Attachments { get; set; } = [];
}
