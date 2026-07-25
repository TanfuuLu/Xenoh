using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Validation;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Competitions;

public sealed record ApplyForOrganizerCommand(string OrganizationName, string ContactEmail, string ContactPhone,
    string? WebsiteUrl, string? Notes, Guid EvidenceFileId) : IRequest<OrganizerProfileDto>;
public sealed record OrganizerEvidenceDto(Guid FileId, string FileName);
public sealed record UploadOrganizerEvidenceCommand(string FileName, string ContentType, long Length, Stream Content) : IRequest<OrganizerEvidenceDto>;
public sealed record GetMyOrganizerProfileQuery : IRequest<OrganizerProfileDto?>;
public sealed record GetOrganizerApplicationsQuery(OrganizerProfileStatus? Status, int Page = 1, int PageSize = 25)
    : IRequest<IReadOnlyList<OrganizerProfileDto>>;
public sealed record GetOrganizerEvidenceUrlQuery(Guid ProfileId) : IRequest<DownloadUrlDto>;
public sealed record ReviewOrganizerApplicationCommand(Guid ProfileId, OrganizerProfileStatus Decision, string Reason)
    : IRequest<OrganizerProfileDto>;

public sealed class ApplyForOrganizerHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ApplyForOrganizerCommand, OrganizerProfileDto>
{
    public async ValueTask<OrganizerProfileDto> Handle(ApplyForOrganizerCommand request, CancellationToken ct)
    {
        var organization = request.OrganizationName.Trim();
        if (organization.Length is < 2 or > 160) throw new InvalidOperationException("Organization name must contain 2 to 160 characters.");
        if (string.IsNullOrWhiteSpace(request.ContactEmail) || request.ContactEmail.Length > 160) throw new InvalidOperationException("A valid contact email is required.");
        var evidenceOwned = await db.StoredFiles.AsNoTracking().AnyAsync(x => x.Id == request.EvidenceFileId && x.OwnerId == currentUser.UserId, ct);
        if (!evidenceOwned) throw new InvalidOperationException("Supporting evidence file was not found.");

        var profile = await db.OrganizerProfiles.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        if (profile?.Status == OrganizerProfileStatus.Suspended) throw new InvalidOperationException("This organizer profile is suspended.");
        if (profile is null)
        {
            profile = new OrganizerProfile { UserId = currentUser.UserId };
            db.OrganizerProfiles.Add(profile);
        }
        profile.OrganizationName = organization;
        profile.ContactEmail = request.ContactEmail.Trim();
        profile.ContactPhone = request.ContactPhone.Trim();
        // Rendered as a link on the admin review screen, so pin the scheme at the write.
        profile.WebsiteUrl = ExternalUrl.NormalizeOrThrow(request.WebsiteUrl, "Website URL");
        profile.Notes = request.Notes?.Trim();
        profile.EvidenceFileId = request.EvidenceFileId;
        if (profile.Status != OrganizerProfileStatus.Approved) profile.Status = OrganizerProfileStatus.Pending;
        profile.ReviewReason = null; profile.ReviewedAt = null; profile.ReviewedById = null; profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(profile);
    }

    internal static OrganizerProfileDto Map(OrganizerProfile x) => new(x.Id, x.OrganizationName, x.ContactEmail, x.ContactPhone,
        x.WebsiteUrl, x.Notes, x.Status, x.EvidenceFileId, x.ReviewedAt, x.ReviewReason);
}

public sealed class UploadOrganizerEvidenceHandler(IApplicationDbContext db, ICurrentUserService currentUser, ICompetitionDocumentStorageService storage)
    : IRequestHandler<UploadOrganizerEvidenceCommand, OrganizerEvidenceDto>
{
    public async ValueTask<OrganizerEvidenceDto> Handle(UploadOrganizerEvidenceCommand request, CancellationToken ct)
    {
        if (request.Length is <= 0 or > 10 * 1024 * 1024) throw new InvalidOperationException("Evidence must be 10 MB or smaller.");
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf")) throw new InvalidOperationException("Evidence must be JPG, PNG, WebP, or PDF.");
        var key = await storage.SaveReceiptAsync(currentUser.UserId, request.FileName, request.ContentType, request.Content, ct);
        var file = new StoredFile { OwnerId = currentUser.UserId, FileName = Path.GetFileName(request.FileName), ContentType = request.ContentType, SizeBytes = request.Length, StorageKey = key };
        db.StoredFiles.Add(file); await db.SaveChangesAsync(ct); return new OrganizerEvidenceDto(file.Id, file.FileName);
    }
}

public sealed class GetMyOrganizerProfileHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyOrganizerProfileQuery, OrganizerProfileDto?>
{
    public async ValueTask<OrganizerProfileDto?> Handle(GetMyOrganizerProfileQuery request, CancellationToken ct)
    {
        var profile = await db.OrganizerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        return profile is null ? null : ApplyForOrganizerHandler.Map(profile);
    }
}

public sealed class GetOrganizerApplicationsHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrganizerApplicationsQuery, IReadOnlyList<OrganizerProfileDto>>
{
    public async ValueTask<IReadOnlyList<OrganizerProfileDto>> Handle(GetOrganizerApplicationsQuery request, CancellationToken ct)
    {
        var query = db.OrganizerProfiles.AsNoTracking();
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        return await query.OrderByDescending(x => x.CreatedAt).Skip((Math.Max(1, request.Page) - 1) * Math.Clamp(request.PageSize, 1, 100))
            .Take(Math.Clamp(request.PageSize, 1, 100)).Select(x => new OrganizerProfileDto(x.Id, x.OrganizationName, x.ContactEmail,
                x.ContactPhone, x.WebsiteUrl, x.Notes, x.Status, x.EvidenceFileId, x.ReviewedAt, x.ReviewReason)).ToListAsync(ct);
    }
}

public sealed class GetOrganizerEvidenceUrlHandler(IApplicationDbContext db, ICompetitionDocumentStorageService storage)
    : IRequestHandler<GetOrganizerEvidenceUrlQuery, DownloadUrlDto>
{
    public async ValueTask<DownloadUrlDto> Handle(GetOrganizerEvidenceUrlQuery request, CancellationToken ct)
    {
        var evidence = await db.OrganizerProfiles.AsNoTracking()
            .Where(x => x.Id == request.ProfileId && x.EvidenceFileId != null)
            .Select(x => new { File = x.EvidenceFile! })
            .Select(x => new { x.File.StorageKey, x.File.FileName })
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Organizer supporting evidence was not found.");

        return new DownloadUrlDto(await storage.GetReceiptUrlAsync(evidence.StorageKey, evidence.FileName, ct));
    }
}

public sealed class ReviewOrganizerApplicationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ReviewOrganizerApplicationCommand, OrganizerProfileDto>
{
    public async ValueTask<OrganizerProfileDto> Handle(ReviewOrganizerApplicationCommand request, CancellationToken ct)
    {
        if (request.Decision is not (OrganizerProfileStatus.Approved or OrganizerProfileStatus.Rejected or OrganizerProfileStatus.Suspended))
            throw new InvalidOperationException("Decision must be Approved, Rejected, or Suspended.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A decision reason is required.");
        var profile = await db.OrganizerProfiles.FirstOrDefaultAsync(x => x.Id == request.ProfileId, ct)
            ?? throw new KeyNotFoundException("Organizer application not found.");
        profile.Status = request.Decision; profile.ReviewReason = request.Reason.Trim(); profile.ReviewedAt = DateTime.UtcNow;
        profile.ReviewedById = currentUser.UserId; profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ApplyForOrganizerHandler.Map(profile);
    }
}
