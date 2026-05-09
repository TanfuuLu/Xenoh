using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class UserAnalysis : BaseEntity
{
    public Guid UserId { get; set; }
    public string Language { get; set; } = "en";
    public string DataFingerprint { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
