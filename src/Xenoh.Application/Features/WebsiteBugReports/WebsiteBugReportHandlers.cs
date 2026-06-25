using System.ComponentModel.DataAnnotations;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Admin;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.WebsiteBugReports;

public sealed record CreateWebsiteBugReportCommand : IRequest<WebsiteBugReportResponse>
{
    [Required]
    [MaxLength(160)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(3000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? PageUrl { get; init; }

    [MaxLength(500)]
    public string? BrowserInfo { get; init; }

    public WebsiteBugReportSeverity Severity { get; init; } = WebsiteBugReportSeverity.Medium;
}

public sealed record GetWebsiteBugReportsQuery(
    WebsiteBugReportStatus? Status,
    WebsiteBugReportSeverity? Severity) : IRequest<List<WebsiteBugReportResponse>>;

public sealed record ReviewWebsiteBugReportCommand : IRequest<WebsiteBugReportResponse>
{
    public Guid BugReportId { get; init; }
    public WebsiteBugReportStatus Status { get; init; }

    [MaxLength(2000)]
    public string? AdminNote { get; init; }
}

public sealed class CreateWebsiteBugReportHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateWebsiteBugReportCommand, WebsiteBugReportResponse>
{
    public async ValueTask<WebsiteBugReportResponse> Handle(CreateWebsiteBugReportCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Bug title is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new InvalidOperationException("Bug description is required.");

        var user = await db.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        var report = new WebsiteBugReport
        {
            UserId = currentUser.UserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            PageUrl = Trim(request.PageUrl, 1000),
            BrowserInfo = Trim(request.BrowserInfo, 500),
            Severity = request.Severity,
            Status = WebsiteBugReportStatus.Open
        };

        db.WebsiteBugReports.Add(report);
        await db.SaveChangesAsync(ct);

        return ToResponse(report, user, null);
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    internal static WebsiteBugReportResponse ToResponse(
        WebsiteBugReport report,
        ApplicationUser user,
        ApplicationUser? reviewedBy) =>
        new(
            report.Id,
            report.UserId,
            FullName(user),
            user.Email ?? string.Empty,
            report.Title,
            report.Description,
            report.PageUrl,
            report.BrowserInfo,
            report.Severity,
            report.Status,
            report.AdminNote,
            report.ReviewedById,
            reviewedBy is null ? null : FullName(reviewedBy),
            report.ReviewedAtUtc,
            report.CreatedAt);

    private static string FullName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? "Unknown user" : name;
    }
}

public sealed class GetWebsiteBugReportsHandler(IApplicationDbContext db)
    : IRequestHandler<GetWebsiteBugReportsQuery, List<WebsiteBugReportResponse>>
{
    public async ValueTask<List<WebsiteBugReportResponse>> Handle(GetWebsiteBugReportsQuery request, CancellationToken ct)
    {
        var query = db.WebsiteBugReports
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.ReviewedBy)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (request.Severity.HasValue)
            query = query.Where(r => r.Severity == request.Severity.Value);

        var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return reports
            .Select(r => CreateWebsiteBugReportHandler.ToResponse(r, r.User, r.ReviewedBy))
            .ToList();
    }
}

public sealed class ReviewWebsiteBugReportHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<ReviewWebsiteBugReportCommand, WebsiteBugReportResponse>
{
    public async ValueTask<WebsiteBugReportResponse> Handle(ReviewWebsiteBugReportCommand request, CancellationToken ct)
    {
        if (request.Status == WebsiteBugReportStatus.Open)
            throw new InvalidOperationException("Bug report review status must be InProgress, Resolved, or Dismissed.");

        var report = await db.WebsiteBugReports
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.BugReportId, ct)
            ?? throw new InvalidOperationException("Bug report not found.");

        var admin = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("Admin not found.");

        var before = $"Status={report.Status}; AdminNote={report.AdminNote ?? "none"}";
        report.Status = request.Status;
        report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
        report.ReviewedById = admin.Id;
        report.ReviewedAtUtc = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        var after = $"Status={report.Status}; AdminNote={report.AdminNote ?? "none"}";

        AdminAudit.Add(
            db,
            currentUser.UserId,
            "ReviewWebsiteBugReport",
            nameof(WebsiteBugReport),
            report.Id,
            report.UserId,
            report.AdminNote ?? $"Bug report marked {report.Status}.",
            before,
            after);

        await db.SaveChangesAsync(ct);
        return CreateWebsiteBugReportHandler.ToResponse(report, report.User, admin);
    }
}
