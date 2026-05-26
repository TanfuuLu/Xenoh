using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Users.Preferences;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Queries.GetMyPreferences;

public sealed class GetMyPreferencesHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyPreferencesQuery, UserPreferencesResponse>
{
    public async ValueTask<UserPreferencesResponse> Handle(GetMyPreferencesQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        return new UserPreferencesResponse(
            UserPreferenceValidator.NormalizeLanguage(user.PreferredLanguage),
            UserPreferenceValidator.NormalizeTheme(user.PreferredTheme),
            UserPreferenceValidator.NormalizeWeightUnit(user.PreferredWeightUnit)
        );
    }
}
