using System.ComponentModel.DataAnnotations;
using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class OrganizerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    [MaxLength(160)] public string OrganizationName { get; set; } = string.Empty;
    [MaxLength(160)] public string ContactEmail { get; set; } = string.Empty;
    [MaxLength(40)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(500)] public string? WebsiteUrl { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }
    public OrganizerProfileStatus Status { get; set; } = OrganizerProfileStatus.Pending;
    public Guid? EvidenceFileId { get; set; }
    public StoredFile? EvidenceFile { get; set; }
    public Guid? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(1000)] public string? ReviewReason { get; set; }
}

public sealed class CompetitionEvent : BaseEntity
{
    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    [MaxLength(120)] public string Slug { get; set; } = string.Empty;
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(5000)] public string Description { get; set; } = string.Empty;
    [MaxLength(500)] public string? BannerUrl { get; set; }
    public CompetitionDiscipline Discipline { get; set; }
    public CompetitionEventStatus Status { get; set; } = CompetitionEventStatus.Draft;
    [MaxLength(200)] public string VenueName { get; set; } = string.Empty;
    [MaxLength(500)] public string Address { get; set; } = string.Empty;
    [MaxLength(80)] public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime RegistrationOpensAtUtc { get; set; }
    public DateTime RegistrationClosesAtUtc { get; set; }
    public int Capacity { get; set; }
    public decimal RegistrationFee { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "VND";
    [MaxLength(160)] public string OrganizerContact { get; set; } = string.Empty;
    [MaxLength(120)] public string? BankName { get; set; }
    [MaxLength(80)] public string? BankAccountNumber { get; set; }
    [MaxLength(160)] public string? BankAccountName { get; set; }
    [MaxLength(1000)] public string? TransferInstructions { get; set; }
    [MaxLength(40)] public string PowerliftingFormulaVersion { get; set; } = "2020";
    public PowerliftingScoringFormula PowerliftingScoringFormula { get; set; } = PowerliftingScoringFormula.Total;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ResultsPublishedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    [MaxLength(1000)] public string? CancellationReason { get; set; }
    public uint Version { get; set; }
    public ICollection<CompetitionCategory> Categories { get; set; } = [];
    public ICollection<CompetitionEventStaff> Staff { get; set; } = [];
    public ICollection<CompetitionRegistration> Registrations { get; set; } = [];

    public bool IsRegistrationOpen(DateTime now) => Status == CompetitionEventStatus.Published && now >= RegistrationOpensAtUtc && now <= RegistrationClosesAtUtc;
}

public sealed class CompetitionEventStaff : BaseEntity
{
    public Guid EventId { get; set; }
    public CompetitionEvent Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public CompetitionStaffPermission Permissions { get; set; }
}

public sealed class CompetitionCategory : BaseEntity
{
    public Guid EventId { get; set; }
    public CompetitionEvent Event { get; set; } = null!;
    [MaxLength(40)] public string Code { get; set; } = string.Empty;
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? EligibilityNotes { get; set; }
    public int Capacity { get; set; }
    public int DisplayOrder { get; set; }
    [MaxLength(40)] public string? SexDivision { get; set; }
    [MaxLength(80)] public string? AgeDivision { get; set; }
    public decimal? MinAge { get; set; }
    public decimal? MaxAge { get; set; }
    public decimal? MinWeightKg { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public decimal? MinHeightCm { get; set; }
    public decimal? MaxHeightCm { get; set; }
    [MaxLength(80)] public string? EquipmentDivision { get; set; }
    [MaxLength(120)] public string? BodybuildingDivision { get; set; }
}

public sealed class CompetitionRegistration : BaseEntity
{
    public Guid EventId { get; set; }
    public CompetitionEvent Event { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public CompetitionCategory Category { get; set; } = null!;
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    [MaxLength(160)] public string AthleteName { get; set; } = string.Empty;
    [MaxLength(160)] public string ContactEmail { get; set; } = string.Empty;
    [MaxLength(40)] public string? ContactPhone { get; set; }
    [MaxLength(300)] public string? ContactFacebook { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(40)] public string? Sex { get; set; }
    public decimal? DeclaredWeightKg { get; set; }
    public decimal? DeclaredHeightCm { get; set; }
    public CompetitionRegistrationStatus Status { get; set; }
    public CompetitionPaymentStatus PaymentStatus { get; set; }
    public decimal ExpectedFee { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "VND";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    [MaxLength(1000)] public string? DecisionReason { get; set; }
    public ICollection<CompetitionPaymentReceipt> Receipts { get; set; } = [];
    public PowerliftingCompetitionResult? PowerliftingResult { get; set; }
    public BodybuildingCompetitionResult? BodybuildingResult { get; set; }
    public bool IsConfirmed => Status == CompetitionRegistrationStatus.Approved && PaymentStatus is CompetitionPaymentStatus.Paid or CompetitionPaymentStatus.NotRequired;
}

public sealed class CompetitionPaymentReceipt : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public CompetitionRegistration Registration { get; set; } = null!;
    public Guid UploadedById { get; set; }
    [MaxLength(255)] public string FileName { get; set; } = string.Empty;
    [MaxLength(100)] public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    [MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    public CompetitionReceiptStatus Status { get; set; } = CompetitionReceiptStatus.UnderReview;
    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
}

public sealed class PowerliftingCompetitionResult : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public CompetitionRegistration Registration { get; set; } = null!;
    public decimal BodyweightKg { get; set; }
    public decimal BestSquatKg { get; set; }
    public decimal BestBenchKg { get; set; }
    public decimal BestDeadliftKg { get; set; }
    public decimal TotalKg { get; set; }
    public PowerliftingScoringFormula Formula { get; set; }
    [MaxLength(40)] public string FormulaVersion { get; set; } = "2020";
    public decimal Score { get; set; }
    public int? Place { get; set; }
    public CompetitionResultState State { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public sealed class BodybuildingCompetitionResult : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public CompetitionRegistration Registration { get; set; } = null!;
    public int? Place { get; set; }
    public CompetitionResultState State { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public sealed class CompetitionAuditLog : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid ActorId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(80)] public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    [MaxLength(4000)] public string? Details { get; set; }
}
