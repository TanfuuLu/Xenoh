using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Competitions;

public sealed record RegisterForCompetitionCommand(Guid EventId, Guid CategoryId, string ContactEmail, string ContactPhone,
    string? ContactFacebook) : IRequest<CompetitionRegistrationDto>;
public sealed record AddGuestCompetitionRegistrationCommand(Guid EventId, Guid CategoryId, string AthleteName, string ContactEmail,
    string ContactPhone, string? ContactFacebook)
    : IRequest<CompetitionRegistrationDto>;
public sealed record GetMyCompetitionRegistrationsQuery : IRequest<IReadOnlyList<CompetitionRegistrationDto>>;
public sealed record GetMyCompetitionRegistrationQuery(Guid EventId) : IRequest<CompetitionRegistrationDto>;
public sealed record GetCompetitionRosterQuery(Guid EventId, CompetitionRegistrationStatus? Status,
    CompetitionPaymentStatus? PaymentStatus, Guid? CategoryId, int Page = 1, int PageSize = 50)
    : IRequest<IReadOnlyList<CompetitionRegistrationDto>>;
public sealed record DecideCompetitionRegistrationCommand(Guid EventId, Guid RegistrationId, bool Approve, string? Reason)
    : IRequest<CompetitionRegistrationDto>;
public sealed record PromoteCompetitionWaitlistCommand(Guid EventId, Guid RegistrationId) : IRequest<CompetitionRegistrationDto>;
public sealed record WithdrawCompetitionRegistrationCommand(Guid EventId) : IRequest;
public sealed record LinkGuestCompetitionRegistrationCommand(Guid EventId, Guid RegistrationId, Guid UserId) : IRequest<CompetitionRegistrationDto>;
public sealed record UploadCompetitionReceiptCommand(Guid EventId, string FileName, string ContentType, long Length, Stream Content)
    : IRequest<CompetitionReceiptDto>;
public sealed record ReviewCompetitionReceiptCommand(Guid EventId, Guid ReceiptId, bool Accept, string? Reason)
    : IRequest<CompetitionRegistrationDto>;
public sealed record GetCompetitionReceiptUrlQuery(Guid EventId, Guid ReceiptId, bool IsAdmin = false) : IRequest<DownloadUrlDto>;

internal static class CompetitionRegistrationRules
{
    public static void ValidateContact(string email, string phone, string? facebook)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 160 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out _))
            throw new InvalidOperationException("A valid contact email is required.");
        if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length is < 7 or > 40)
            throw new InvalidOperationException("A contact phone number containing 7 to 40 characters is required.");
        if (facebook?.Trim().Length > 300) throw new InvalidOperationException("Facebook contact must not exceed 300 characters.");
    }

    public static async Task<CompetitionRegistrationStatus> InitialStatusAsync(IApplicationDbContext db, CompetitionEvent e, CompetitionCategory c, CancellationToken ct)
    {
        var consuming = new[] { CompetitionRegistrationStatus.Submitted, CompetitionRegistrationStatus.Approved };
        var eventCount = await db.CompetitionRegistrations.AsNoTracking().CountAsync(x => x.EventId == e.Id && consuming.Contains(x.Status), ct);
        var categoryCount = await db.CompetitionRegistrations.AsNoTracking().CountAsync(x => x.CategoryId == c.Id && consuming.Contains(x.Status), ct);
        return eventCount >= e.Capacity || categoryCount >= c.Capacity ? CompetitionRegistrationStatus.Waitlisted : CompetitionRegistrationStatus.Submitted;
    }

    public static IQueryable<CompetitionRegistration> IncludeAll(IApplicationDbContext db) => db.CompetitionRegistrations
        .Include(x => x.Event).Include(x => x.Category).Include(x => x.Receipts);
}

public sealed class RegisterForCompetitionHandler(IApplicationDbContext db, ICurrentUserService currentUser,
    IDistributedLock distributedLock, INotificationService notifications)
    : IRequestHandler<RegisterForCompetitionCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(RegisterForCompetitionCommand request, CancellationToken ct)
    {
        var e = await db.CompetitionEvents.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Competition event not found.");
        if (!e.IsRegistrationOpen(DateTime.UtcNow)) throw new InvalidOperationException("Registration is not open.");
        if (await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.EventId == e.Id && x.UserId == currentUser.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct))
            throw new InvalidOperationException("You already have an active registration for this event.");
        var c = e.Categories.FirstOrDefault(x => x.Id == request.CategoryId) ?? throw new InvalidOperationException("Category does not belong to this event.");
        CompetitionRegistrationRules.ValidateContact(request.ContactEmail, request.ContactPhone, request.ContactFacebook);
        var user = await db.ApplicationUsers.AsNoTracking().FirstAsync(x => x.Id == currentUser.UserId, ct);
        await using var capacityLock = await distributedLock.TryAcquireAsync($"competition:{e.Id}:capacity", TimeSpan.FromSeconds(10), ct);
        var status = await CompetitionRegistrationRules.InitialStatusAsync(db, e, c, ct);
        var registration = New(e, c, user.Id, $"{user.FirstName} {user.LastName}".Trim(), request.ContactEmail,
            request.ContactPhone, request.ContactFacebook, status);
        db.CompetitionRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
        await notifications.NotifyAsync(e.OwnerId, "CompetitionRegistrationSubmitted", $"{registration.AthleteName} applied to {e.Title}.", e.Id, "CompetitionEvent", ct);
        var loaded = await CompetitionRegistrationRules.IncludeAll(db).AsNoTracking().FirstAsync(x => x.Id == registration.Id, ct);
        return CompetitionAccess.MapRegistration(loaded);
    }

    internal static CompetitionRegistration New(CompetitionEvent e, CompetitionCategory c, Guid? userId, string name, string email,
        string phone, string? facebook, CompetitionRegistrationStatus status) => new()
    {
        EventId = e.Id, CategoryId = c.Id, UserId = userId, AthleteName = name, ContactEmail = email.Trim(), ContactPhone = phone.Trim(),
        ContactFacebook = string.IsNullOrWhiteSpace(facebook) ? null : facebook.Trim(), Status = status,
        PaymentStatus = e.RegistrationFee == 0 ? CompetitionPaymentStatus.NotRequired : CompetitionPaymentStatus.AwaitingReceipt,
        ExpectedFee = e.RegistrationFee, Currency = e.Currency, SubmittedAt = DateTime.UtcNow
    };
}

public sealed class AddGuestCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDistributedLock distributedLock)
    : IRequestHandler<AddGuestCompetitionRegistrationCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(AddGuestCompetitionRegistrationCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageRegistrations, ct);
        var e = await db.CompetitionEvents.Include(x => x.Categories).FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status is CompetitionEventStatus.Completed or CompetitionEventStatus.Cancelled) throw new InvalidOperationException("Registrations are closed.");
        var c = e.Categories.FirstOrDefault(x => x.Id == request.CategoryId) ?? throw new InvalidOperationException("Category does not belong to this event.");
        if (request.AthleteName.Trim().Length < 2) throw new InvalidOperationException("Guest name is required.");
        CompetitionRegistrationRules.ValidateContact(request.ContactEmail, request.ContactPhone, request.ContactFacebook);
        if (await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.EventId == e.Id && x.UserId == null && x.ContactEmail.ToLower() == request.ContactEmail.Trim().ToLower() && x.Status != CompetitionRegistrationStatus.Withdrawn, ct))
            throw new InvalidOperationException("A guest with this email is already registered.");
        await using var capacityLock = await distributedLock.TryAcquireAsync($"competition:{e.Id}:capacity", TimeSpan.FromSeconds(10), ct);
        var status = await CompetitionRegistrationRules.InitialStatusAsync(db, e, c, ct);
        var registration = RegisterForCompetitionHandler.New(e, c, null, request.AthleteName.Trim(), request.ContactEmail, request.ContactPhone,
            request.ContactFacebook, status);
        db.CompetitionRegistrations.Add(registration); CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "GuestRegistrationCreated", "CompetitionRegistration", registration.Id);
        await db.SaveChangesAsync(ct);
        registration.Event = e; registration.Category = c; return CompetitionAccess.MapRegistration(registration);
    }
}

public sealed class GetMyCompetitionRegistrationsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyCompetitionRegistrationsQuery, IReadOnlyList<CompetitionRegistrationDto>>
{
    public async ValueTask<IReadOnlyList<CompetitionRegistrationDto>> Handle(GetMyCompetitionRegistrationsQuery request, CancellationToken ct)
    {
        var rows = await CompetitionRegistrationRules.IncludeAll(db).AsNoTracking().Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.SubmittedAt).ToListAsync(ct);
        return rows.Select(CompetitionAccess.MapRegistration).ToList();
    }
}

public sealed class GetMyCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyCompetitionRegistrationQuery, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(GetMyCompetitionRegistrationQuery request, CancellationToken ct)
    {
        var row = await CompetitionRegistrationRules.IncludeAll(db).AsNoTracking().FirstOrDefaultAsync(x => x.EventId == request.EventId && x.UserId == currentUser.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct)
            ?? throw new KeyNotFoundException("Registration not found.");
        return CompetitionAccess.MapRegistration(row);
    }
}

public sealed class GetCompetitionRosterHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCompetitionRosterQuery, IReadOnlyList<CompetitionRegistrationDto>>
{
    public async ValueTask<IReadOnlyList<CompetitionRegistrationDto>> Handle(GetCompetitionRosterQuery request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAnyAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageRegistrations
            | CompetitionStaffPermission.ReviewPayments | CompetitionStaffPermission.ManageResults, ct);
        var query = CompetitionRegistrationRules.IncludeAll(db).AsNoTracking().Where(x => x.EventId == request.EventId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.PaymentStatus.HasValue) query = query.Where(x => x.PaymentStatus == request.PaymentStatus);
        if (request.CategoryId.HasValue) query = query.Where(x => x.CategoryId == request.CategoryId);
        var rows = await query.OrderBy(x => x.Status == CompetitionRegistrationStatus.Waitlisted ? 0 : 1).ThenBy(x => x.SubmittedAt)
            .Skip((Math.Max(1, request.Page) - 1) * Math.Clamp(request.PageSize, 1, 100)).Take(Math.Clamp(request.PageSize, 1, 100)).ToListAsync(ct);
        return rows.Select(CompetitionAccess.MapRegistration).ToList();
    }
}

public sealed class DecideCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications,
    IDistributedLock distributedLock) : IRequestHandler<DecideCompetitionRegistrationCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(DecideCompetitionRegistrationCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageRegistrations, ct);
        await using var capacityLock = await distributedLock.TryAcquireAsync($"competition:{request.EventId}:capacity", TimeSpan.FromSeconds(10), ct);
        var r = await CompetitionRegistrationRules.IncludeAll(db).FirstOrDefaultAsync(x => x.Id == request.RegistrationId && x.EventId == request.EventId, ct)
            ?? throw new KeyNotFoundException("Registration not found.");
        if (r.Status == CompetitionRegistrationStatus.Waitlisted && request.Approve) throw new InvalidOperationException("Promote the waitlisted registration before approval.");
        if (r.Status is CompetitionRegistrationStatus.Withdrawn or CompetitionRegistrationStatus.Rejected) throw new InvalidOperationException("Registration cannot be reviewed in its current state.");
        if (request.Approve)
        {
            if (r.ExpectedFee > 0 && r.PaymentStatus != CompetitionPaymentStatus.Paid)
                throw new InvalidOperationException("Verify an accepted payment receipt before approving this registration.");
            var active = await db.CompetitionRegistrations.AsNoTracking().CountAsync(x => x.EventId == r.EventId && x.Id != r.Id && x.Status == CompetitionRegistrationStatus.Approved, ct);
            if (active >= r.Event.Capacity) throw new InvalidOperationException("Event capacity has been reached.");
            r.Status = CompetitionRegistrationStatus.Approved;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A rejection reason is required.");
            r.Status = CompetitionRegistrationStatus.Rejected;
        }
        r.DecisionReason = request.Reason?.Trim(); r.ReviewedAt = DateTime.UtcNow; r.ReviewedById = currentUser.UserId; r.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, r.EventId, currentUser.UserId, request.Approve ? "RegistrationApproved" : "RegistrationRejected", "CompetitionRegistration", r.Id, r.DecisionReason);
        await db.SaveChangesAsync(ct);
        if (r.UserId.HasValue) await notifications.NotifyAsync(r.UserId.Value, request.Approve ? "CompetitionRegistrationApproved" : "CompetitionRegistrationRejected",
            request.Approve ? $"Your registration for {r.Event.Title} was approved." : $"Your registration for {r.Event.Title} was rejected: {r.DecisionReason}", r.EventId, "CompetitionEvent", ct);
        return CompetitionAccess.MapRegistration(r);
    }
}

public sealed class PromoteCompetitionWaitlistHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications,
    IDistributedLock distributedLock) : IRequestHandler<PromoteCompetitionWaitlistCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(PromoteCompetitionWaitlistCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageRegistrations, ct);
        await using var capacityLock = await distributedLock.TryAcquireAsync($"competition:{request.EventId}:capacity", TimeSpan.FromSeconds(10), ct);
        var r = await CompetitionRegistrationRules.IncludeAll(db).FirstOrDefaultAsync(x => x.Id == request.RegistrationId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Registration not found.");
        if (r.Status != CompetitionRegistrationStatus.Waitlisted) throw new InvalidOperationException("Only a waitlisted registration may be promoted.");
        var next = await db.CompetitionRegistrations.AsNoTracking().Where(x => x.EventId == r.EventId && x.Status == CompetitionRegistrationStatus.Waitlisted)
            .OrderBy(x => x.SubmittedAt).Select(x => x.Id).FirstAsync(ct);
        if (next != r.Id) throw new InvalidOperationException("Promote registrations in waitlist order.");
        var status = await CompetitionRegistrationRules.InitialStatusAsync(db, r.Event, r.Category, ct);
        if (status == CompetitionRegistrationStatus.Waitlisted) throw new InvalidOperationException("No capacity is currently available.");
        r.Status = CompetitionRegistrationStatus.Submitted; r.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, r.EventId, currentUser.UserId, "WaitlistPromoted", "CompetitionRegistration", r.Id);
        await db.SaveChangesAsync(ct);
        if (r.UserId.HasValue) await notifications.NotifyAsync(r.UserId.Value, "CompetitionWaitlistPromoted", $"A place opened for {r.Event.Title}. Your application is ready for review.", r.EventId, "CompetitionEvent", ct);
        return CompetitionAccess.MapRegistration(r);
    }
}

public sealed class WithdrawCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<WithdrawCompetitionRegistrationCommand>
{
    public async ValueTask<Unit> Handle(WithdrawCompetitionRegistrationCommand request, CancellationToken ct)
    {
        var r = await db.CompetitionRegistrations.Include(x => x.Event).FirstOrDefaultAsync(x => x.EventId == request.EventId && x.UserId == currentUser.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct)
            ?? throw new KeyNotFoundException("Registration not found.");
        if (r.Event.Status == CompetitionEventStatus.Completed) throw new InvalidOperationException("Completed registrations cannot be withdrawn.");
        r.Status = CompetitionRegistrationStatus.Withdrawn; r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class LinkGuestCompetitionRegistrationHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<LinkGuestCompetitionRegistrationCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(LinkGuestCompetitionRegistrationCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageRegistrations, ct);
        var r = await CompetitionRegistrationRules.IncludeAll(db).FirstOrDefaultAsync(x => x.Id == request.RegistrationId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Registration not found.");
        if (r.UserId.HasValue) throw new InvalidOperationException("Registration is already linked.");
        if (!await db.ApplicationUsers.AsNoTracking().AnyAsync(x => x.Id == request.UserId, ct)) throw new KeyNotFoundException("User not found.");
        if (await db.CompetitionRegistrations.AsNoTracking().AnyAsync(x => x.EventId == request.EventId && x.UserId == request.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct))
            throw new InvalidOperationException("This user already has a registration for the event.");
        r.UserId = request.UserId; r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return CompetitionAccess.MapRegistration(r);
    }
}

public sealed class UploadCompetitionReceiptHandler(IApplicationDbContext db, ICurrentUserService currentUser,
    ICompetitionDocumentStorageService storage, INotificationService notifications)
    : IRequestHandler<UploadCompetitionReceiptCommand, CompetitionReceiptDto>
{
    public async ValueTask<CompetitionReceiptDto> Handle(UploadCompetitionReceiptCommand request, CancellationToken ct)
    {
        if (request.Length is <= 0 or > 10 * 1024 * 1024) throw new InvalidOperationException("Receipt must be 10 MB or smaller.");
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf")) throw new InvalidOperationException("Receipt must be JPG, PNG, WebP, or PDF.");
        var r = await db.CompetitionRegistrations.Include(x => x.Event).Include(x => x.Receipts)
            .FirstOrDefaultAsync(x => x.EventId == request.EventId && x.UserId == currentUser.UserId && x.Status != CompetitionRegistrationStatus.Withdrawn, ct)
            ?? throw new KeyNotFoundException("Registration not found.");
        if (r.ExpectedFee <= 0) throw new InvalidOperationException("This event does not require payment.");
        if (r.PaymentStatus == CompetitionPaymentStatus.Paid) throw new InvalidOperationException("Payment is already verified.");
        if (r.Receipts.Any(x => x.Status == CompetitionReceiptStatus.UnderReview)) throw new InvalidOperationException("A receipt is already under review.");
        var key = await storage.SaveReceiptAsync(currentUser.UserId, request.FileName, request.ContentType, request.Content, ct);
        var receipt = new CompetitionPaymentReceipt { RegistrationId = r.Id, UploadedById = currentUser.UserId, FileName = Path.GetFileName(request.FileName), ContentType = request.ContentType, SizeBytes = request.Length, StorageKey = key };
        db.CompetitionPaymentReceipts.Add(receipt); r.PaymentStatus = CompetitionPaymentStatus.UnderReview; r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifications.NotifyAsync(r.Event.OwnerId, "CompetitionReceiptSubmitted", $"A payment receipt was submitted for {r.Event.Title}.", r.EventId, "CompetitionEvent", ct);
        return new CompetitionReceiptDto(receipt.Id, receipt.FileName, receipt.ContentType, receipt.SizeBytes, receipt.Status, receipt.CreatedAt, null, null);
    }
}

public sealed class ReviewCompetitionReceiptHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<ReviewCompetitionReceiptCommand, CompetitionRegistrationDto>
{
    public async ValueTask<CompetitionRegistrationDto> Handle(ReviewCompetitionReceiptCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ReviewPayments, ct);
        var receipt = await db.CompetitionPaymentReceipts.Include(x => x.Registration).ThenInclude(x => x.Event)
            .FirstOrDefaultAsync(x => x.Id == request.ReceiptId && x.Registration.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Receipt not found.");
        if (receipt.Status != CompetitionReceiptStatus.UnderReview) throw new InvalidOperationException("Receipt was already reviewed.");
        if (!request.Accept && string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A rejection reason is required.");
        receipt.Status = request.Accept ? CompetitionReceiptStatus.Accepted : CompetitionReceiptStatus.Rejected;
        receipt.ReviewedAt = DateTime.UtcNow; receipt.ReviewedById = currentUser.UserId; receipt.RejectionReason = request.Accept ? null : request.Reason!.Trim();
        receipt.Registration.PaymentStatus = request.Accept ? CompetitionPaymentStatus.Paid : CompetitionPaymentStatus.ReceiptRejected;
        if (!request.Accept && receipt.Registration.Status == CompetitionRegistrationStatus.Approved)
        {
            receipt.Registration.Status = CompetitionRegistrationStatus.Submitted;
            receipt.Registration.ReviewedAt = null;
            receipt.Registration.ReviewedById = null;
            receipt.Registration.DecisionReason = null;
        }
        receipt.Registration.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, request.Accept ? "ReceiptAccepted" : "ReceiptRejected", "CompetitionPaymentReceipt", receipt.Id, receipt.RejectionReason);
        await db.SaveChangesAsync(ct);
        if (receipt.Registration.UserId.HasValue) await notifications.NotifyAsync(receipt.Registration.UserId.Value, request.Accept ? "CompetitionPaymentAccepted" : "CompetitionPaymentRejected",
            request.Accept ? $"Payment for {receipt.Registration.Event.Title} was verified." : $"Payment receipt was rejected: {receipt.RejectionReason}", request.EventId, "CompetitionEvent", ct);
        var loaded = await CompetitionRegistrationRules.IncludeAll(db).AsNoTracking().FirstAsync(x => x.Id == receipt.RegistrationId, ct);
        return CompetitionAccess.MapRegistration(loaded);
    }
}

public sealed class GetCompetitionReceiptUrlHandler(IApplicationDbContext db, ICurrentUserService currentUser, ICompetitionDocumentStorageService storage)
    : IRequestHandler<GetCompetitionReceiptUrlQuery, DownloadUrlDto>
{
    public async ValueTask<DownloadUrlDto> Handle(GetCompetitionReceiptUrlQuery request, CancellationToken ct)
    {
        var receipt = await db.CompetitionPaymentReceipts.AsNoTracking().Include(x => x.Registration)
            .FirstOrDefaultAsync(x => x.Id == request.ReceiptId && x.Registration.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Receipt not found.");
        var own = receipt.Registration.UserId == currentUser.UserId;
        var permission = request.IsAdmin ? CompetitionStaffPermission.ReviewPayments : await CompetitionAccess.GetPermissionsAsync(db, request.EventId, currentUser.UserId, ct);
        if (!own && (permission & CompetitionStaffPermission.ReviewPayments) == 0) throw new UnauthorizedAccessException();
        return new DownloadUrlDto(await storage.GetReceiptUrlAsync(receipt.StorageKey, receipt.FileName, ct));
    }
}
