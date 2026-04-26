using Mediator;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;

namespace Xenoh.Application.Features.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileResponse>;
