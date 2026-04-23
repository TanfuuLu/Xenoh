using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<UserProfileResponse>;
