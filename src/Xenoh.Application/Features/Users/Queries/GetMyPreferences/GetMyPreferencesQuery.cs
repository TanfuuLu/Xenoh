using Mediator;
using Xenoh.Application.Features.Users.Preferences;

namespace Xenoh.Application.Features.Users.Queries.GetMyPreferences;

public sealed record GetMyPreferencesQuery : IRequest<UserPreferencesResponse>;
