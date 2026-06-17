using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class Friendship : BaseEntity
{
    public Guid UserAId { get; set; }
    public ApplicationUser UserA { get; set; } = null!;

    public Guid UserBId { get; set; }
    public ApplicationUser UserB { get; set; } = null!;

    public Guid RequesterId { get; set; }
    public ApplicationUser Requester { get; set; } = null!;

    public Guid AddresseeId { get; set; }
    public ApplicationUser Addressee { get; set; } = null!;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime? RespondedAt { get; set; }

    public static (Guid UserAId, Guid UserBId) NormalizePair(Guid firstUserId, Guid secondUserId) =>
        firstUserId.CompareTo(secondUserId) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);
}
