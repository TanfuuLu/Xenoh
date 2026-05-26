namespace Xenoh.Application.Features.Users.Preferences;

public sealed record UserPreferencesResponse(
    string Language,
    string Theme,
    string WeightUnit
);
