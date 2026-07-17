using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Community;

public sealed record CommunitySettingsResponse(string StatsVisibility);
public sealed record GetCommunitySettingsQuery : IRequest<CommunitySettingsResponse>;
public sealed record UpdateCommunitySettingsCommand(string StatsVisibility) : IRequest<CommunitySettingsResponse>;

public sealed class GetCommunitySettingsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCommunitySettingsQuery, CommunitySettingsResponse>
{
    public async ValueTask<CommunitySettingsResponse> Handle(GetCommunitySettingsQuery request, CancellationToken ct)
    {
        var value = await db.CommunitySettings.AsNoTracking()
            .Where(x => x.UserId == currentUser.UserId)
            .Select(x => (CommunityStatsVisibility?)x.StatsVisibility)
            .FirstOrDefaultAsync(ct) ?? CommunityStatsVisibility.Friends;
        return new(value.ToString());
    }
}

public sealed class UpdateCommunitySettingsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCommunitySettingsCommand, CommunitySettingsResponse>
{
    public async ValueTask<CommunitySettingsResponse> Handle(UpdateCommunitySettingsCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CommunityStatsVisibility>(request.StatsVisibility, true, out var visibility))
            throw new InvalidOperationException("Stats visibility must be Friends or OnlyMe.");

        var settings = await db.CommunitySettings.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        if (settings is null)
        {
            settings = new CommunitySettings { UserId = currentUser.UserId, StatsVisibility = visibility };
            db.CommunitySettings.Add(settings);
        }
        else
        {
            settings.StatsVisibility = visibility;
        }

        await db.SaveChangesAsync(ct);
        return new(visibility.ToString());
    }
}
