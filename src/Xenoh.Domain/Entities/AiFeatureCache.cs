using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class AiFeatureCache : BaseEntity
{
    public string Feature { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public Guid UserId { get; set; }
    public Guid? SubjectUserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string DataFingerprint { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
