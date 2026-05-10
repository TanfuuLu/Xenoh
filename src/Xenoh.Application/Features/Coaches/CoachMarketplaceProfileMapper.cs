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
            profile.CoachingStyle
        );
    }

    public static List<CoachPackageDto> ToPackageDtos(IEnumerable<CoachPackage>? packages)
    {
        return packages?
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .Select(p => new CoachPackageDto(
                p.Id,
                p.Name,
                p.PriceAmount,
                p.Currency,
                p.DurationLabel,
                p.Description,
                p.Type,
                p.DisplayOrder))
            .ToList() ?? [];
    }

    public static decimal? StartingPackagePrice(IEnumerable<CoachPackage>? packages)
    {
        var prices = packages?
            .Where(p => p.PriceAmount.HasValue)
            .Select(p => p.PriceAmount!.Value)
            .ToList();

        return prices is { Count: > 0 } ? prices.Min() : null;
    }
}
