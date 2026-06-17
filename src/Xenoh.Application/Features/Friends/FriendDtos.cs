namespace Xenoh.Application.Features.Friends;

public sealed record FriendResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? Bio,
    DateTime FriendsSince);

public sealed record FriendRequestResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string Direction,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt);
