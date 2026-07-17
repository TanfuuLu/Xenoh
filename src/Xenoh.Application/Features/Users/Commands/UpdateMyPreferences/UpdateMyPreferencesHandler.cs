using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Users.Preferences;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyPreferences;

public sealed class UpdateMyPreferencesHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateMyPreferencesCommand, UserPreferencesResponse>
{
    public async ValueTask<UserPreferencesResponse> Handle(UpdateMyPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        user.PreferredLanguage = UserPreferenceValidator.NormalizeLanguage(request.Language);
        user.PreferredTheme = UserPreferenceValidator.NormalizeTheme(request.Theme);
        user.PreferredWeightUnit = UserPreferenceValidator.NormalizeWeightUnit(request.WeightUnit);
        if (request.TrackRpe.HasValue)
            user.TrackRpe = request.TrackRpe.Value;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        return new UserPreferencesResponse(
            user.PreferredLanguage,
            user.PreferredTheme,
            user.PreferredWeightUnit,
            user.TrackRpe
        );
    }
}
