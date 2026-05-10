using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Coaches;

public static class CoachMarketplaceProfileMapper
{
    public static CoachMarketplaceProfileDto? ToDto(CoachMarketplaceProfile? profile)
    {
        if (profile is null) return null;

        return new CoachMarketplaceProfileDto(
            profile.Headline,
            profile.ExperienceYears,
            profile.Specialties,
            profile.Certifications,
            profile.Languages,
            profile.CoachingMethods,
            profile.Achievements,
            profile.ClientResultsSummary,
            profile.Availability,
            profile.ResponseTime,
            profile.CoachingStyle,
            profile.MonthlyPriceAmount,
            profile.SessionPriceAmount,
            profile.Currency
        );
    }

    public static decimal? StartingPrice(CoachMarketplaceProfile? profile)
    {
        var prices = new[] { profile?.MonthlyPriceAmount, profile?.SessionPriceAmount }
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .ToList();

        return prices is { Count: > 0 } ? prices.Min() : null;
    }
}
