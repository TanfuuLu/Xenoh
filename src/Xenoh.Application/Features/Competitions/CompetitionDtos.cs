using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Competitions;

public sealed record OrganizerProfileDto(Guid Id, string OrganizationName, string ContactEmail, string ContactPhone,
    string? WebsiteUrl, string? Notes, OrganizerProfileStatus Status, Guid? EvidenceFileId, DateTime? ReviewedAt, string? ReviewReason);

public sealed record CompetitionCategoryDto(Guid Id, string Code, string Name, string? EligibilityNotes, int Capacity, int DisplayOrder,
    string? SexDivision, string? AgeDivision, decimal? MinAge, decimal? MaxAge, decimal? MinWeightKg, decimal? MaxWeightKg,
    decimal? MinHeightCm, decimal? MaxHeightCm, string? EquipmentDivision, string? BodybuildingDivision);

public sealed record CompetitionEventSummaryDto(Guid Id, string Slug, string Title, CompetitionDiscipline Discipline,
    CompetitionEventStatus Status, string VenueName, string Address, DateTime StartsAtUtc, DateTime EndsAtUtc,
    decimal RegistrationFee, string Currency, int Capacity, int ConfirmedCount, string? BannerUrl);

public sealed record CompetitionEventDto(Guid Id, string Slug, string Title, string Description, string? BannerUrl,
    CompetitionDiscipline Discipline, CompetitionEventStatus Status, string VenueName, string Address, string TimeZoneId,
    DateTime StartsAtUtc, DateTime EndsAtUtc, DateTime RegistrationOpensAtUtc, DateTime RegistrationClosesAtUtc,
    int Capacity, decimal RegistrationFee, string Currency, string OrganizerContact, string? BankName, string? BankAccountNumber,
    string? BankAccountName, string? TransferInstructions, PowerliftingScoringFormula PowerliftingScoringFormula,
    string PowerliftingFormulaVersion, DateTime? ResultsPublishedAt, string? CancellationReason,
    int ConfirmedCount, bool CanManage, CompetitionStaffPermission Permissions, IReadOnlyList<CompetitionCategoryDto> Categories);

public sealed record CompetitionRegistrationDto(Guid Id, Guid EventId, string EventTitle, string EventSlug,
    CompetitionEventStatus EventStatus, DateTime EventEndsAtUtc, Guid CategoryId,
    string CategoryName, Guid? UserId, string AthleteName, string ContactEmail, string? ContactPhone, string? ContactFacebook, DateOnly? DateOfBirth,
    string? Sex, decimal? DeclaredWeightKg, decimal? DeclaredHeightCm, CompetitionRegistrationStatus Status, CompetitionPaymentStatus PaymentStatus, bool IsConfirmed,
    decimal ExpectedFee, string Currency, DateTime SubmittedAt, string? DecisionReason,
    IReadOnlyList<CompetitionReceiptDto> Receipts);

public sealed record CompetitionReceiptDto(Guid Id, string FileName, string ContentType, long SizeBytes,
    CompetitionReceiptStatus Status, DateTime CreatedAt, DateTime? ReviewedAt, string? RejectionReason);

public sealed record CompetitionResultDto(Guid RegistrationId, string AthleteName, string CategoryName,
    CompetitionResultState State, int? Place, decimal? BodyweightKg, decimal? BestSquatKg, decimal? BestBenchKg,
    decimal? BestDeadliftKg, decimal? TotalKg, decimal? Score, PowerliftingScoringFormula? Formula,
    string? FormulaVersion, string? Notes);

public sealed record AdminCompetitionSummaryDto(Guid Id, string Slug, string Title, CompetitionDiscipline Discipline,
    CompetitionEventStatus Status, DateTime StartsAtUtc, DateTime EndsAtUtc, DateTime RegistrationClosesAtUtc,
    int Capacity, int ConfirmedCount, string OrganizerName, string OrganizerEmail, DateTime? ResultsPublishedAt, string? CancellationReason);

// Wraps the shared summary rather than widening it: CompetitionEventSummaryDto also serves the public list.
public sealed record ManagedCompetitionSummaryDto(CompetitionEventSummaryDto Event, bool IsOwner, CompetitionStaffPermission Permissions);

public sealed record CompetitionStaffCandidateDto(Guid UserId, string FullName, string Email, string? AvatarUrl,
    bool IsOwner, bool IsStaff, CompetitionStaffPermission Permissions);

public sealed record CompetitionPageDto<T>(IReadOnlyList<T> Items, string? NextCursor);
public sealed record DownloadUrlDto(string Url);
