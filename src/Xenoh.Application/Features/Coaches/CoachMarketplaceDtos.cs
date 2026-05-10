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
    string? CoachingStyle
);

public sealed record CoachPackageDto(
    Guid Id,
    string Name,
    decimal? PriceAmount,
    string Currency,
    string DurationLabel,
    string? Description,
    string? Type,
    int DisplayOrder
);

public sealed record CoachPackageInput(
    string Name,
    decimal? PriceAmount,
    string? Currency,
    string DurationLabel,
    string? Description,
    string? Type,
    int DisplayOrder
);
