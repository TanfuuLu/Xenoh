using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Competitions;

internal static class CompetitionAccess
{
    public static async Task<CompetitionStaffPermission> GetPermissionsAsync(IApplicationDbContext db, Guid eventId, Guid userId, CancellationToken ct)
    {
        var ownerId = await db.CompetitionEvents.AsNoTracking().Where(x => x.Id == eventId).Select(x => x.OwnerId).FirstOrDefaultAsync(ct);
        if (ownerId == Guid.Empty) throw new KeyNotFoundException("Competition event not found.");
        if (ownerId == userId) return CompetitionStaffPermission.All;
        return await db.CompetitionEventStaff.AsNoTracking().Where(x => x.EventId == eventId && x.UserId == userId)
            .Select(x => x.Permissions).FirstOrDefaultAsync(ct);
    }

    public static async Task RequireAsync(IApplicationDbContext db, Guid eventId, Guid userId, CompetitionStaffPermission required, CancellationToken ct)
    {
        var actual = await GetPermissionsAsync(db, eventId, userId, ct);
        if ((actual & required) != required) throw new UnauthorizedAccessException("You do not have permission for this event operation.");
    }

    public static async Task RequireApprovedOrganizerAsync(IApplicationDbContext db, Guid userId, CancellationToken ct)
    {
        var approved = await db.OrganizerProfiles.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Status == OrganizerProfileStatus.Approved, ct);
        if (!approved) throw new UnauthorizedAccessException("An approved organizer profile is required.");
    }

    public static CompetitionCategoryDto MapCategory(CompetitionCategory x) => new(x.Id, x.Code, x.Name, x.EligibilityNotes,
        x.Capacity, x.DisplayOrder, x.SexDivision, x.AgeDivision, x.MinAge, x.MaxAge, x.MinWeightKg, x.MaxWeightKg,
        x.MinHeightCm, x.MaxHeightCm, x.EquipmentDivision, x.BodybuildingDivision);

    public static CompetitionRegistrationDto MapRegistration(CompetitionRegistration x) => new(x.Id, x.EventId, x.Event.Title,
        x.Event.Slug, x.CategoryId, x.Category.Name, x.UserId, x.AthleteName, x.ContactEmail, x.ContactPhone, x.ContactFacebook, x.DateOfBirth, x.Sex,
        x.DeclaredWeightKg, x.DeclaredHeightCm, x.Status, x.PaymentStatus, x.IsConfirmed, x.ExpectedFee, x.Currency, x.SubmittedAt, x.DecisionReason,
        x.Receipts.OrderByDescending(r => r.CreatedAt).Select(r => new CompetitionReceiptDto(r.Id, r.FileName, r.ContentType,
            r.SizeBytes, r.Status, r.CreatedAt, r.ReviewedAt, r.RejectionReason)).ToList());

    public static void Audit(IApplicationDbContext db, Guid eventId, Guid actorId, string action, string entityType, Guid? entityId, string? details = null) =>
        db.CompetitionAuditLogs.Add(new CompetitionAuditLog { EventId = eventId, ActorId = actorId, Action = action, EntityType = entityType, EntityId = entityId, Details = details });
}
