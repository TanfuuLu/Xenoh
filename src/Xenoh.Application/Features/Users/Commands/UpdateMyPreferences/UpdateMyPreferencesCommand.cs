using Mediator;
using Xenoh.Application.Features.Users.Preferences;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyPreferences;

public sealed record UpdateMyPreferencesCommand(
    string? Language,
    string? Theme,
    string? WeightUnit,
    bool? TrackRpe
) : IRequest<UserPreferencesResponse>;
