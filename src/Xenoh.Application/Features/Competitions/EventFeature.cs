using System.Globalization;
using System.Text;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Competitions;

public sealed record CompetitionEventInput(string Title, string Description, string? BannerUrl, CompetitionDiscipline Discipline,
    string VenueName, string Address, string TimeZoneId, DateTime StartsAtUtc, DateTime EndsAtUtc,
    DateTime RegistrationOpensAtUtc, DateTime RegistrationClosesAtUtc, int Capacity, decimal RegistrationFee,
    string Currency, string OrganizerContact, string? BankName, string? BankAccountNumber, string? BankAccountName,
    string? TransferInstructions, PowerliftingScoringFormula PowerliftingScoringFormula);

public sealed record CreateCompetitionEventCommand(CompetitionEventInput Input) : IRequest<CompetitionEventDto>;
public sealed record UpdateCompetitionEventCommand(Guid EventId, CompetitionEventInput Input) : IRequest<CompetitionEventDto>;
public sealed record GetPublicCompetitionEventsQuery(CompetitionDiscipline? Discipline, CompetitionEventStatus? Status,
    DateTime? StartsAfterUtc, string? Location, string? Cursor, int PageSize = 20) : IRequest<CompetitionPageDto<CompetitionEventSummaryDto>>;
public sealed record GetCompetitionEventBySlugQuery(string Slug) : IRequest<CompetitionEventDto>;
public sealed record GetManagedCompetitionEventsQuery : IRequest<IReadOnlyList<ManagedCompetitionSummaryDto>>;
public sealed record PublishCompetitionEventCommand(Guid EventId) : IRequest<CompetitionEventDto>;
public sealed record CloseCompetitionRegistrationCommand(Guid EventId) : IRequest;
public sealed record CancelCompetitionEventCommand(Guid EventId, string Reason) : IRequest;
public sealed record DeleteCompetitionEventCommand(Guid EventId) : IRequest;

public sealed record CategoryInput(string Code, string Name, string? EligibilityNotes, int Capacity, int DisplayOrder,
    string? SexDivision, string? AgeDivision, decimal? MinAge, decimal? MaxAge, decimal? MinWeightKg, decimal? MaxWeightKg,
    decimal? MinHeightCm, decimal? MaxHeightCm, string? EquipmentDivision, string? BodybuildingDivision);
public sealed record UpsertCompetitionCategoryCommand(Guid EventId, Guid? CategoryId, CategoryInput Input) : IRequest<CompetitionCategoryDto>;
public sealed record DeleteCompetitionCategoryCommand(Guid EventId, Guid CategoryId) : IRequest;
public sealed record ClearCompetitionCategoriesCommand(Guid EventId) : IRequest;
public sealed record SetCompetitionStaffCommand(Guid EventId, Guid UserId, CompetitionStaffPermission Permissions) : IRequest;
public sealed record RemoveCompetitionStaffCommand(Guid EventId, Guid UserId) : IRequest;
public sealed record FindCompetitionStaffCandidateQuery(Guid EventId, string Email) : IRequest<CompetitionStaffCandidateDto>;
public sealed record AdvanceCompetitionLifecycleCommand : IRequest<int>;
public sealed record EndCompetitionEventCommand(Guid EventId, string? Reason) : IRequest;
public sealed record AdminEndCompetitionEventCommand(Guid EventId, string? Reason) : IRequest;
public sealed record AdminCancelCompetitionEventCommand(Guid EventId, string Reason) : IRequest;
public sealed record GetAdminCompetitionEventsQuery(CompetitionEventStatus? Status) : IRequest<IReadOnlyList<AdminCompetitionSummaryDto>>;

internal static class CompetitionEventMapping
{
    public static CompetitionEventDto Map(CompetitionEvent x, Guid currentUserId, CompetitionStaffPermission permissions, bool exposePayment) =>
        new(x.Id, x.Slug, x.Title, x.Description, x.BannerUrl, x.Discipline, x.Status, x.VenueName, x.Address, x.TimeZoneId,
            x.StartsAtUtc, x.EndsAtUtc, x.RegistrationOpensAtUtc, x.RegistrationClosesAtUtc, x.Capacity, x.RegistrationFee,
            x.Currency, x.OrganizerContact, exposePayment ? x.BankName : null, exposePayment ? x.BankAccountNumber : null,
            exposePayment ? x.BankAccountName : null, exposePayment ? x.TransferInstructions : null, x.PowerliftingScoringFormula,
            x.PowerliftingFormulaVersion, x.ResultsPublishedAt, x.CancellationReason, ConfirmedCount(x), permissions != CompetitionStaffPermission.None,
            permissions, x.Categories.OrderBy(c => c.DisplayOrder).Select(CompetitionAccess.MapCategory).ToList());

    public static CompetitionEventSummaryDto Summary(CompetitionEvent x) => new(x.Id, x.Slug, x.Title, x.Discipline, x.Status,
        x.VenueName, x.Address, x.StartsAtUtc, x.EndsAtUtc, x.RegistrationFee, x.Currency, x.Capacity,
        ConfirmedCount(x), x.BannerUrl);

    internal static int ConfirmedCount(CompetitionEvent x) => x.Registrations.Count(r =>
        r.Status == CompetitionRegistrationStatus.Approved &&
        (r.PaymentStatus == CompetitionPaymentStatus.Paid || r.PaymentStatus == CompetitionPaymentStatus.NotRequired));
}

public sealed class CreateCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateCompetitionEventCommand, CompetitionEventDto>
{
    public async ValueTask<CompetitionEventDto> Handle(CreateCompetitionEventCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireApprovedOrganizerAsync(db, currentUser.UserId, ct);
        ValidateInput(request.Input);
        var slugRoot = Slugify(request.Input.Title);
        var slug = slugRoot;
        for (var i = 2; await db.CompetitionEvents.AsNoTracking().AnyAsync(x => x.Slug == slug, ct); i++) slug = $"{slugRoot}-{i}";
        var e = new CompetitionEvent { OwnerId = currentUser.UserId, Slug = slug };
        Apply(e, request.Input, allowDiscipline: true);
        db.CompetitionEvents.Add(e);
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "EventCreated", "CompetitionEvent", e.Id);
        await db.SaveChangesAsync(ct);
        return CompetitionEventMapping.Map(e, currentUser.UserId, CompetitionStaffPermission.All, true);
    }

    internal static void ValidateInput(CompetitionEventInput x)
    {
        if (x.Title.Trim().Length is < 3 or > 160) throw new InvalidOperationException("Title must contain 3 to 160 characters.");
        if (x.StartsAtUtc >= x.EndsAtUtc) throw new InvalidOperationException("Event end must be after event start.");
        if (x.RegistrationOpensAtUtc >= x.RegistrationClosesAtUtc || x.RegistrationClosesAtUtc > x.StartsAtUtc)
            throw new InvalidOperationException("Registration dates must be ordered and close before the event starts.");
        if (x.Capacity is < 1 or > 10000) throw new InvalidOperationException("Capacity must be between 1 and 10,000.");
        if (x.RegistrationFee < 0) throw new InvalidOperationException("Registration fee cannot be negative.");
        if (x.Currency.Trim().Length != 3) throw new InvalidOperationException("Currency must be a three-letter ISO code.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId); } catch { throw new InvalidOperationException("Unknown event timezone."); }
    }

    internal static void Apply(CompetitionEvent e, CompetitionEventInput x, bool allowDiscipline)
    {
        e.Title = x.Title.Trim(); e.Description = x.Description.Trim(); e.BannerUrl = x.BannerUrl?.Trim();
        if (allowDiscipline) e.Discipline = x.Discipline;
        e.VenueName = x.VenueName.Trim(); e.Address = x.Address.Trim(); e.TimeZoneId = x.TimeZoneId;
        e.StartsAtUtc = x.StartsAtUtc; e.EndsAtUtc = x.EndsAtUtc; e.RegistrationOpensAtUtc = x.RegistrationOpensAtUtc;
        e.RegistrationClosesAtUtc = x.RegistrationClosesAtUtc; e.Capacity = x.Capacity; e.RegistrationFee = x.RegistrationFee;
        e.Currency = x.Currency.Trim().ToUpperInvariant(); e.OrganizerContact = x.OrganizerContact.Trim();
        e.BankName = x.BankName?.Trim(); e.BankAccountNumber = x.BankAccountNumber?.Trim(); e.BankAccountName = x.BankAccountName?.Trim();
        e.TransferInstructions = x.TransferInstructions?.Trim(); e.PowerliftingScoringFormula = x.PowerliftingScoringFormula;
        e.PowerliftingFormulaVersion = PowerliftingScoreCalculator.FormulaVersion; e.UpdatedAt = DateTime.UtcNow;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(); var dash = false;
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) { sb.Append(char.ToLowerInvariant(ch)); dash = false; }
            else if (!dash && sb.Length > 0) { sb.Append('-'); dash = true; }
        }
        var slug = sb.ToString().Trim('-'); return slug.Length == 0 ? $"event-{Guid.NewGuid():N}" : slug[..Math.Min(100, slug.Length)];
    }
}

public sealed class UpdateCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCompetitionEventCommand, CompetitionEventDto>
{
    public async ValueTask<CompetitionEventDto> Handle(UpdateCompetitionEventCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.EditEvent, ct);
        CreateCompetitionEventHandler.ValidateInput(request.Input);
        var e = await db.CompetitionEvents.Include(x => x.Categories).Include(x => x.Registrations).FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled) throw new InvalidOperationException("Completed or cancelled events cannot be edited.");
        var disciplineChanged = e.Discipline != request.Input.Discipline;
        if (disciplineChanged && e.Status != CompetitionEventStatus.Draft) throw new InvalidOperationException("Discipline is immutable after publication.");
        if (disciplineChanged && e.Registrations.Count != 0) throw new InvalidOperationException("Discipline cannot change after registrations exist.");
        if (disciplineChanged)
        {
            db.CompetitionCategories.RemoveRange(e.Categories);
            e.Categories = [];
        }
        CreateCompetitionEventHandler.Apply(e, request.Input, e.Status == CompetitionEventStatus.Draft);
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "EventUpdated", "CompetitionEvent", e.Id);
        await db.SaveChangesAsync(ct);
        var permissions = await CompetitionAccess.GetPermissionsAsync(db, e.Id, currentUser.UserId, ct);
        return CompetitionEventMapping.Map(e, currentUser.UserId, permissions, true);
    }
}

public sealed class GetPublicCompetitionEventsHandler(IApplicationDbContext db)
    : IRequestHandler<GetPublicCompetitionEventsQuery, CompetitionPageDto<CompetitionEventSummaryDto>>
{
    public async ValueTask<CompetitionPageDto<CompetitionEventSummaryDto>> Handle(GetPublicCompetitionEventsQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.PageSize, 1, 50);
        var query = db.CompetitionEvents.AsNoTracking().Include(x => x.Registrations)
            .Where(x => x.Status != CompetitionEventStatus.Draft);
        if (request.Discipline.HasValue) query = query.Where(x => x.Discipline == request.Discipline);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.StartsAfterUtc.HasValue) query = query.Where(x => x.StartsAtUtc >= request.StartsAfterUtc);
        if (!string.IsNullOrWhiteSpace(request.Location)) query = query.Where(x => x.Address.ToLower().Contains(request.Location.ToLower()) || x.VenueName.ToLower().Contains(request.Location.ToLower()));
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var parts = request.Cursor.Split(':', 2);
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParse(parts[1], out var cursorId))
            {
                var cursorDate = new DateTime(ticks, DateTimeKind.Utc);
                query = query.Where(x => x.StartsAtUtc > cursorDate || x.StartsAtUtc == cursorDate && x.Id.CompareTo(cursorId) > 0);
            }
        }
        var items = await query.OrderBy(x => x.StartsAtUtc).ThenBy(x => x.Id).Take(take + 1).ToListAsync(ct);
        var hasMore = items.Count > take; if (hasMore) items.RemoveAt(items.Count - 1);
        return new CompetitionPageDto<CompetitionEventSummaryDto>(items.Select(CompetitionEventMapping.Summary).ToList(),
            hasMore ? $"{items[^1].StartsAtUtc.Ticks}:{items[^1].Id}" : null);
    }
}

public sealed class GetCompetitionEventBySlugHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCompetitionEventBySlugQuery, CompetitionEventDto>
{
    public async ValueTask<CompetitionEventDto> Handle(GetCompetitionEventBySlugQuery request, CancellationToken ct)
    {
        var e = await db.CompetitionEvents.AsNoTracking().Include(x => x.Categories).Include(x => x.Registrations).FirstOrDefaultAsync(x => x.Slug == request.Slug, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        var permissions = currentUser.IsAuthenticated ? await CompetitionAccess.GetPermissionsAsync(db, e.Id, currentUser.UserId, ct) : CompetitionStaffPermission.None;
        if (e.Status == CompetitionEventStatus.Draft && permissions == CompetitionStaffPermission.None) throw new KeyNotFoundException("Competition event not found.");
        var registered = currentUser.IsAuthenticated && await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.EventId == e.Id && x.UserId == currentUser.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct);
        return CompetitionEventMapping.Map(e, currentUser.UserId, permissions, registered || permissions != CompetitionStaffPermission.None);
    }
}

public sealed class GetManagedCompetitionEventsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetManagedCompetitionEventsQuery, IReadOnlyList<ManagedCompetitionSummaryDto>>
{
    public async ValueTask<IReadOnlyList<ManagedCompetitionSummaryDto>> Handle(GetManagedCompetitionEventsQuery request, CancellationToken ct)
    {
        var items = await db.CompetitionEvents.AsNoTracking().Include(x => x.Registrations)
            .Where(x => x.OwnerId == currentUser.UserId || x.Staff.Any(s => s.UserId == currentUser.UserId))
            .Select(x => new
            {
                Event = x,
                IsOwner = x.OwnerId == currentUser.UserId,
                Permissions = x.Staff.Where(s => s.UserId == currentUser.UserId).Select(s => s.Permissions).FirstOrDefault(),
            })
            .OrderByDescending(x => x.Event.StartsAtUtc).ToListAsync(ct);
        return items.Select(x => new ManagedCompetitionSummaryDto(CompetitionEventMapping.Summary(x.Event), x.IsOwner,
            x.IsOwner ? CompetitionStaffPermission.All : x.Permissions)).ToList();
    }
}

public sealed class PublishCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser, ISubscriptionService subscriptions)
    : IRequestHandler<PublishCompetitionEventCommand, CompetitionEventDto>
{
    public async ValueTask<CompetitionEventDto> Handle(PublishCompetitionEventCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.PublishEvent, ct);
        var e = await db.CompetitionEvents.Include(x => x.Categories).Include(x => x.Registrations).FirstAsync(x => x.Id == request.EventId, ct);
        await CompetitionAccess.RequireApprovedOrganizerAsync(db, e.OwnerId, ct);
        // Staff may publish on the owner's behalf, but only while the owner's Organizer plan is active.
        if (await subscriptions.GetActiveTierAsync(e.OwnerId, ct) != PlanTier.Organizer)
            throw new UnauthorizedAccessException("The event owner needs an active Organizer plan to publish this event.");
        if (e.Status != CompetitionEventStatus.Draft) throw new InvalidOperationException("Only draft events can be published.");
        if (e.Categories.Count == 0) throw new InvalidOperationException("Add at least one competition category.");
        if (e.RegistrationFee > 0 && (string.IsNullOrWhiteSpace(e.BankName) || string.IsNullOrWhiteSpace(e.BankAccountNumber) || string.IsNullOrWhiteSpace(e.BankAccountName)))
            throw new InvalidOperationException("Paid events require complete bank transfer information.");
        e.Status = CompetitionEventStatus.Published; e.PublishedAt = DateTime.UtcNow; e.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "EventPublished", "CompetitionEvent", e.Id);
        await db.SaveChangesAsync(ct);
        return CompetitionEventMapping.Map(e, currentUser.UserId, await CompetitionAccess.GetPermissionsAsync(db, e.Id, currentUser.UserId, ct), true);
    }
}

public sealed class CloseCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CloseCompetitionRegistrationCommand>
{
    public async ValueTask<Unit> Handle(CloseCompetitionRegistrationCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.PublishEvent, ct);
        var e = await db.CompetitionEvents.FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status != CompetitionEventStatus.Published) throw new InvalidOperationException("Registration can only be closed for a published event.");
        e.Status = CompetitionEventStatus.RegistrationClosed; e.RegistrationClosesAtUtc = DateTime.UtcNow;
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "RegistrationClosed", "CompetitionEvent", e.Id);
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class CancelCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<CancelCompetitionEventCommand>
{
    public async ValueTask<Unit> Handle(CancelCompetitionEventCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.PublishEvent, ct);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("Cancellation reason is required.");
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled) throw new InvalidOperationException("Event cannot be cancelled in its current state.");
        e.Status = CompetitionEventStatus.Cancelled; e.CancelledAt = DateTime.UtcNow; e.CancellationReason = request.Reason.Trim();
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "EventCancelled", "CompetitionEvent", e.Id, request.Reason.Trim());
        await db.SaveChangesAsync(ct);
        foreach (var userId in e.Registrations.Where(x => x.UserId.HasValue && x.Status is not (CompetitionRegistrationStatus.Rejected or CompetitionRegistrationStatus.Withdrawn)).Select(x => x.UserId!.Value).Distinct())
            await notifications.NotifyAsync(userId, "CompetitionEventCancelled", $"{e.Title} was cancelled. {e.CancellationReason}", e.Id, "CompetitionEvent", ct);
        return Unit.Value;
    }
}

public sealed class DeleteCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteCompetitionEventCommand>
{
    public async ValueTask<Unit> Handle(DeleteCompetitionEventCommand request, CancellationToken ct)
    {
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstOrDefaultAsync(x => x.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        if (e.OwnerId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (e.Status != CompetitionEventStatus.Draft || e.Registrations.Count != 0) throw new InvalidOperationException("Only empty draft events may be deleted.");
        db.CompetitionEvents.Remove(e); await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class UpsertCompetitionCategoryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertCompetitionCategoryCommand, CompetitionCategoryDto>
{
    public async ValueTask<CompetitionCategoryDto> Handle(UpsertCompetitionCategoryCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageCategories, ct);
        var e = await db.CompetitionEvents.AsNoTracking().FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled) throw new InvalidOperationException("Categories can no longer be changed.");
        if (request.Input.Code.Trim().Length is < 1 or > 40 || request.Input.Name.Trim().Length is < 2 or > 160 || request.Input.Capacity < 1)
            throw new InvalidOperationException("Category code, name, and positive capacity are required.");
        var c = request.CategoryId.HasValue
            ? await db.CompetitionCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Category not found.")
            : new CompetitionCategory { EventId = request.EventId };
        if (!request.CategoryId.HasValue) db.CompetitionCategories.Add(c);
        c.Code = request.Input.Code.Trim().ToUpperInvariant(); c.Name = request.Input.Name.Trim(); c.EligibilityNotes = request.Input.EligibilityNotes?.Trim();
        c.Capacity = request.Input.Capacity; c.DisplayOrder = request.Input.DisplayOrder; c.SexDivision = request.Input.SexDivision?.Trim();
        c.AgeDivision = request.Input.AgeDivision?.Trim(); c.MinAge = request.Input.MinAge; c.MaxAge = request.Input.MaxAge;
        c.MinWeightKg = request.Input.MinWeightKg; c.MaxWeightKg = request.Input.MaxWeightKg; c.MinHeightCm = request.Input.MinHeightCm;
        c.MaxHeightCm = request.Input.MaxHeightCm; c.EquipmentDivision = request.Input.EquipmentDivision?.Trim(); c.BodybuildingDivision = request.Input.BodybuildingDivision?.Trim();
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, request.CategoryId.HasValue ? "CategoryUpdated" : "CategoryCreated", "CompetitionCategory", c.Id);
        await db.SaveChangesAsync(ct); return CompetitionAccess.MapCategory(c);
    }
}

public sealed class DeleteCompetitionCategoryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteCompetitionCategoryCommand>
{
    public async ValueTask<Unit> Handle(DeleteCompetitionCategoryCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageCategories, ct);
        var c = await db.CompetitionCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Category not found.");
        if (await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.CategoryId == c.Id, ct)) throw new InvalidOperationException("A category with registrations cannot be deleted.");
        db.CompetitionCategories.Remove(c); await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class ClearCompetitionCategoriesHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ClearCompetitionCategoriesCommand>
{
    public async ValueTask<Unit> Handle(ClearCompetitionCategoriesCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageCategories, ct);
        var e = await db.CompetitionEvents.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled)
            throw new InvalidOperationException("Categories can no longer be changed.");
        if (await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.EventId == request.EventId, ct))
            throw new InvalidOperationException("Categories cannot be cleared after registrations exist.");
        db.CompetitionCategories.RemoveRange(e.Categories);
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, "CategoriesCleared", "CompetitionEvent", request.EventId);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class SetCompetitionStaffHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetCompetitionStaffCommand>
{
    public async ValueTask<Unit> Handle(SetCompetitionStaffCommand request, CancellationToken ct)
    {
        var callerPermissions = await CompetitionAccess.GetPermissionsAsync(db, request.EventId, currentUser.UserId, ct);
        if ((callerPermissions & CompetitionStaffPermission.ManageStaff) == 0) throw new UnauthorizedAccessException();
        if ((request.Permissions & ~callerPermissions) != 0) throw new UnauthorizedAccessException("You cannot grant permissions you do not possess.");
        var e = await db.CompetitionEvents.AsNoTracking().FirstAsync(x => x.Id == request.EventId, ct);
        if (request.UserId == e.OwnerId) throw new InvalidOperationException("The event owner already has every permission.");
        if (!await db.ApplicationUsers.AsNoTracking().AnyAsync(x => x.Id == request.UserId, ct)) throw new KeyNotFoundException("User not found.");
        var staff = await db.CompetitionEventStaff.FirstOrDefaultAsync(x => x.EventId == request.EventId && x.UserId == request.UserId, ct);
        if (staff is null) { staff = new CompetitionEventStaff { EventId = request.EventId, UserId = request.UserId }; db.CompetitionEventStaff.Add(staff); }
        staff.Permissions = request.Permissions; staff.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, "StaffPermissionsChanged", "CompetitionEventStaff", staff.Id, request.Permissions.ToString());
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

/// <summary>
/// Clock-driven lifecycle: the registration window closing stops new entries, and the event's end
/// date closes the whole event. Publishing results still completes an event early.
/// </summary>
public sealed class AdvanceCompetitionLifecycleHandler(IApplicationDbContext db) : IRequestHandler<AdvanceCompetitionLifecycleCommand, int>
{
    public async ValueTask<int> Handle(AdvanceCompetitionLifecycleCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var changed = 0;

        var toClose = await db.CompetitionEvents
            .Where(x => x.Status == CompetitionEventStatus.Published && now > x.RegistrationClosesAtUtc && now <= x.EndsAtUtc)
            .ToListAsync(ct);
        foreach (var e in toClose)
        {
            e.Status = CompetitionEventStatus.RegistrationClosed; e.UpdatedAt = now;
            CompetitionAccess.Audit(db, e.Id, Guid.Empty, "RegistrationClosedAutomatically", "CompetitionEvent", e.Id,
                $"Registration window ended {e.RegistrationClosesAtUtc:O}.");
            changed++;
        }

        var toComplete = await db.CompetitionEvents
            .Where(x => (x.Status == CompetitionEventStatus.Published || x.Status == CompetitionEventStatus.RegistrationClosed) && now > x.EndsAtUtc)
            .ToListAsync(ct);
        foreach (var e in toComplete)
        {
            e.Status = CompetitionEventStatus.Completed; e.UpdatedAt = now;
            CompetitionAccess.Audit(db, e.Id, Guid.Empty, "EventCompletedAutomatically", "CompetitionEvent", e.Id,
                $"Event ended {e.EndsAtUtc:O}.");
            changed++;
        }

        if (changed > 0) await db.SaveChangesAsync(ct);
        return changed;
    }
}

public sealed class GetAdminCompetitionEventsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminCompetitionEventsQuery, IReadOnlyList<AdminCompetitionSummaryDto>>
{
    public async ValueTask<IReadOnlyList<AdminCompetitionSummaryDto>> Handle(GetAdminCompetitionEventsQuery request, CancellationToken ct)
    {
        var query = db.CompetitionEvents.AsNoTracking().Include(x => x.Owner).Include(x => x.Registrations).AsQueryable();
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        var items = await query.OrderByDescending(x => x.StartsAtUtc).Take(200).ToListAsync(ct);
        return items.Select(x => new AdminCompetitionSummaryDto(x.Id, x.Slug, x.Title, x.Discipline, x.Status, x.StartsAtUtc,
            x.EndsAtUtc, x.RegistrationClosesAtUtc, x.Capacity, CompetitionEventMapping.ConfirmedCount(x),
            $"{x.Owner.FirstName} {x.Owner.LastName}".Trim(), x.Owner.Email ?? string.Empty, x.ResultsPublishedAt, x.CancellationReason)).ToList();
    }
}

internal static class CompetitionCompletion
{
    /// <summary>Closes an event for good. Used by the organizer's own "End event" and by the admin override.</summary>
    public static async Task CompleteAsync(IApplicationDbContext db, INotificationService notifications, CompetitionEvent e,
        Guid actorId, string action, string? reason, CancellationToken ct)
    {
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled)
            throw new InvalidOperationException("This event has already finished.");
        if (e.Status == CompetitionEventStatus.Draft)
            throw new InvalidOperationException("A draft event has not started. Delete or cancel it instead.");
        e.Status = CompetitionEventStatus.Completed; e.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, e.Id, actorId, action, "CompetitionEvent", e.Id, reason?.Trim());
        await db.SaveChangesAsync(ct);
        foreach (var userId in e.Registrations.Where(x => x.UserId.HasValue && x.Status == CompetitionRegistrationStatus.Approved)
            .Select(x => x.UserId!.Value).Distinct())
            await notifications.NotifyAsync(userId, "CompetitionEventCompleted", $"{e.Title} has been closed.", e.Id, "CompetitionEvent", ct);
    }
}

public sealed class EndCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<EndCompetitionEventCommand>
{
    public async ValueTask<Unit> Handle(EndCompetitionEventCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.PublishEvent, ct);
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstAsync(x => x.Id == request.EventId, ct);
        await CompetitionCompletion.CompleteAsync(db, notifications, e, currentUser.UserId, "EventEnded", request.Reason, ct);
        return Unit.Value;
    }
}

public sealed class AdminEndCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<AdminEndCompetitionEventCommand>
{
    public async ValueTask<Unit> Handle(AdminEndCompetitionEventCommand request, CancellationToken ct)
    {
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstOrDefaultAsync(x => x.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        await CompetitionCompletion.CompleteAsync(db, notifications, e, currentUser.UserId, "EventEndedByAdmin", request.Reason, ct);
        return Unit.Value;
    }
}

public sealed class AdminCancelCompetitionEventHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<AdminCancelCompetitionEventCommand>
{
    public async ValueTask<Unit> Handle(AdminCancelCompetitionEventCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A cancellation reason is required.");
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstOrDefaultAsync(x => x.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled)
            throw new InvalidOperationException("This event has already finished.");
        e.Status = CompetitionEventStatus.Cancelled; e.CancellationReason = request.Reason.Trim(); e.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "EventCancelledByAdmin", "CompetitionEvent", e.Id, e.CancellationReason);
        await db.SaveChangesAsync(ct);
        foreach (var userId in e.Registrations.Where(x => x.UserId.HasValue && x.Status != CompetitionRegistrationStatus.Withdrawn)
            .Select(x => x.UserId!.Value).Distinct())
            await notifications.NotifyAsync(userId, "CompetitionEventCancelled", $"{e.Title} was cancelled: {e.CancellationReason}", e.Id, "CompetitionEvent", ct);
        return Unit.Value;
    }
}

public sealed class FindCompetitionStaffCandidateHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<FindCompetitionStaffCandidateQuery, CompetitionStaffCandidateDto>
{
    public async ValueTask<CompetitionStaffCandidateDto> Handle(FindCompetitionStaffCandidateQuery request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageStaff, ct);
        var email = (request.Email ?? string.Empty).Trim();
        if (email.Length == 0) throw new InvalidOperationException("An email address is required.");
        var normalized = email.ToUpperInvariant();
        var user = await db.ApplicationUsers.AsNoTracking()
            .Where(x => x.NormalizedEmail == normalized)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email, x.AvatarUrl })
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("No Xenoh account uses this email address.");
        var ownerId = await db.CompetitionEvents.AsNoTracking().Where(x => x.Id == request.EventId).Select(x => x.OwnerId).FirstAsync(ct);
        var staff = await db.CompetitionEventStaff.AsNoTracking().Where(x => x.EventId == request.EventId && x.UserId == user.Id)
            .Select(x => (CompetitionStaffPermission?)x.Permissions).FirstOrDefaultAsync(ct);
        return new CompetitionStaffCandidateDto(user.Id, $"{user.FirstName} {user.LastName}".Trim(), user.Email ?? email,
            user.AvatarUrl, user.Id == ownerId, staff.HasValue, staff ?? CompetitionStaffPermission.None);
    }
}

public sealed class RemoveCompetitionStaffHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RemoveCompetitionStaffCommand>
{
    public async ValueTask<Unit> Handle(RemoveCompetitionStaffCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageStaff, ct);
        var staff = await db.CompetitionEventStaff.FirstOrDefaultAsync(x => x.EventId == request.EventId && x.UserId == request.UserId, ct) ?? throw new KeyNotFoundException("Staff member not found.");
        db.CompetitionEventStaff.Remove(staff); await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
