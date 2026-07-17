using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class CommunitySettings
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public CommunityStatsVisibility StatsVisibility { get; set; } = CommunityStatsVisibility.Friends;
}
