using Mediator;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyAvatar;

public sealed record UpdateMyAvatarCommand(
    string FileName,
    string ContentType,
    long Length,
    Stream Content
) : IRequest<UserProfileResponse>;
