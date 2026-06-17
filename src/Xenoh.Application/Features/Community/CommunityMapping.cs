using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Community;

internal static class CommunityMapping
{
    public static string FullName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? "Athlete" : fullName;
    }

    public static Guid OtherUserId(Friendship friendship, Guid userId) =>
        friendship.UserAId == userId ? friendship.UserBId : friendship.UserAId;

    public static string RequestDirection(Friendship friendship, Guid userId) =>
        friendship.RequesterId == userId ? "outgoing" : "incoming";
}
