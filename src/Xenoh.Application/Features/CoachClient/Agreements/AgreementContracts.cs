using System.ComponentModel.DataAnnotations;
using Mapster;
using Mediator;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Agreements;

public sealed record AgreementTerms
{
    [Required, StringLength(120)] public string Title { get; init; } = "";
    [Required, StringLength(2000)] public string Goals { get; init; } = "";
    [Required, StringLength(2000)] public string Services { get; init; } = "";
    [Required, StringLength(300)] public string CheckInFrequency { get; init; } = "";
    [Required, StringLength(500)] public string Availability { get; init; } = "";
    [Required, StringLength(2000)] public string CoachResponsibilities { get; init; } = "";
    [Required, StringLength(2000)] public string ClientResponsibilities { get; init; } = "";
    [Range(1, 30)] public int NoticeDays { get; init; } = 7;

    public CoachingAgreement Publish(Guid coachId, DateOnly start, DateOnly end, Guid publisher)
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);
        if (new[] { Title, Goals, Services, CheckInFrequency, Availability, CoachResponsibilities, ClientResponsibilities }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Complete all coaching details before publishing.");
        if (end <= start || end.Year > 2100 || start < CoachingPolicy.LocalDate(DateTime.UtcNow))
            throw new InvalidOperationException("Choose a future end date after the coaching start date.");
        var agreement = this.Adapt<CoachingAgreement>();
        agreement.CoachId = coachId;
        agreement.StartDate = start;
        agreement.EndDate = end;
        agreement.PublishedBy = publisher;
        agreement.PublishedAtUtc = DateTime.UtcNow;
        return agreement;
    }
}

public sealed record AgreementDto(
    Guid Id, int Version, string Title, string Goals, string Services, string CheckInFrequency,
    string Availability, string CoachResponsibilities, string ClientResponsibilities,
    string TimeZone, string PolicyVersion, DateOnly StartDate, DateOnly EndDate, int NoticeDays,
    Guid PublishedBy, DateTime PublishedAtUtc, Guid? AcceptedBy, DateTime? AcceptedAtUtc, DateTime? RejectedAtUtc);

public sealed record AgreementPreviewResponse(Guid CoachId, string CoachName, AgreementDto Agreement);
public sealed record PreviewAgreementQuery(string Code) : IRequest<AgreementPreviewResponse>;
public sealed record GetAgreementQuery(Guid RelationshipId) : IRequest<RelationshipAgreementResponse>;
public sealed record ListAgreementsQuery(int Page = 1) : IRequest<IReadOnlyList<RelationshipAgreementSummary>>;
public sealed record RelationshipAgreementSummary(Guid Id, string CoachName, string ClientName, string Status, DateOnly StartDate, DateOnly? EndDate);
public sealed record AgreementEventDto(Guid Id, string Kind, Guid? ActorId, DateTime OccurredAtUtc, DateTime? EffectiveAtUtc);
public sealed record RelationshipAgreementResponse(
    Guid Id, Guid CoachId, string CoachName, Guid ClientId, string ClientName, string Status,
    Guid Revision, DateOnly StartDate, DateOnly? EndDate, DateTime? NoticeEndsAtUtc,
    bool HasCoachingAccess, Guid? CurrentAgreementId, IReadOnlyList<AgreementDto> Agreements, IReadOnlyList<AgreementEventDto> Events, bool HasPendingCoachEndingRequest);

public sealed record PreviewEndingQuery(Guid RelationshipId, bool Immediate = false) : IRequest<EndingPreviewResponse>;
public sealed record EndingPreviewResponse(Guid Revision, DateTime RequestedAtUtc, DateTime EffectiveAtUtc, bool Immediate, int NoticeDays);

public sealed record ProposeAgreementCommand : IRequest<AgreementDto>
{
    public Guid RelationshipId { get; init; }
    public Guid ExpectedRevision { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    [Required] public AgreementTerms Terms { get; init; } = new();
    public bool Acknowledged { get; init; }
}

public sealed record RespondToAgreementCommand(Guid RelationshipId, Guid AgreementId, Guid ExpectedRevision, bool Accept, bool Acknowledged) : IRequest;
