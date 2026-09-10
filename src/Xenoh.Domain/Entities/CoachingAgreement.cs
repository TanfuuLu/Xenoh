using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

/// <summary>Published terms are append-only. A renewal is a separate version.</summary>
public sealed class CoachingAgreement : BaseEntity
{
    public Guid CoachId { get; set; }
    public Guid? RelationshipId { get; set; }
    public int Version { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string CheckInFrequency { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string CoachResponsibilities { get; set; } = string.Empty;
    public string ClientResponsibilities { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = "coaching-v1";
    public string TimeZone { get; set; } = "Asia/Bangkok";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NoticeDays { get; set; }
    public Guid PublishedBy { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public Guid? AcceptedBy { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
}
