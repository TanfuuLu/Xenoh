using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class ProgressPhoto : BaseEntity
{
    public Guid ProgressCheckInId { get; set; }
    public ProgressCheckIn ProgressCheckIn { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public ProgressPhotoAngle Angle { get; set; }
    public int SortOrder { get; set; }
}
