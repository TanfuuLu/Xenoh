using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class AiChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public AiChatConversation Conversation { get; set; } = null!;

    public AiChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}
