namespace Xenoh.Application.Features.Coaches;

public sealed record CoachMarketplaceProfileDto(
    string? Headline,
    int? ExperienceYears,
    string[] Specialties,
    string[] Certifications,
    string[] Languages,
    string[] CoachingMethods,
    string[] Achievements,
    string? ClientResultsSummary,
    string? Availability,
    string? ResponseTime,
    string? CoachingStyle,
    decimal? MonthlyPriceAmount,
    decimal? SessionPriceAmount,
    string Currency
);
