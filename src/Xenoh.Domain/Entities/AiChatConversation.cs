using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class AiChatConversation : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime? SummaryCutoffAt { get; set; }
    public int MessageCount { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }

    public ICollection<AiChatMessage> Messages { get; set; } = [];
}
