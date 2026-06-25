using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.WebsiteAnalytics;

public sealed record TrackWebsiteActivityCommand(
    WebsiteActivityEventType EventType,
    string SessionId,
    string Path,
    string? PreviousPath,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    int? DurationSeconds,
    string? UserAgent,
    Guid? UserId) : IRequest;

public sealed class TrackWebsiteActivityHandler(IApplicationDbContext db)
    : IRequestHandler<TrackWebsiteActivityCommand>
{
    public async ValueTask<Unit> Handle(TrackWebsiteActivityCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            throw new InvalidOperationException("Session id is required.");

        if (string.IsNullOrWhiteSpace(request.Path))
            throw new InvalidOperationException("Path is required.");

        var duration = request.DurationSeconds;
        if (duration.HasValue)
            duration = Math.Clamp(duration.Value, 0, 3600);

        db.WebsiteActivityEvents.Add(new WebsiteActivityEvent
        {
            UserId = request.UserId,
            EventType = request.EventType,
            SessionId = Trim(request.SessionId, 100) ?? string.Empty,
            Path = Trim(request.Path, 500) ?? "/",
            PreviousPath = Trim(request.PreviousPath, 500),
            Referrer = Trim(request.Referrer, 1000),
            UtmSource = Trim(request.UtmSource, 120),
            UtmMedium = Trim(request.UtmMedium, 120),
            UtmCampaign = Trim(request.UtmCampaign, 200),
            DurationSeconds = duration,
            UserAgent = Trim(request.UserAgent, 500),
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
