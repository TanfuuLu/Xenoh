using Mediator;

namespace Xenoh.Application.Features.Community.Queries.GetCommunityUserProfile;

public sealed record GetCommunityUserProfileQuery(Guid UserId) : IRequest<CommunityUserProfileResponse>;
