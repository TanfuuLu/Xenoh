using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class ChatMessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    // R2 object key (private bucket; only reachable via presigned URL).
    public string StorageKey { get; set; } = string.Empty;
}
