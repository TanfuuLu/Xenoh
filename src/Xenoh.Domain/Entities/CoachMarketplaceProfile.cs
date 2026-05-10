namespace Xenoh.Domain.Entities;

public class CoachMarketplaceProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CoachId { get; set; }

    public string? Headline { get; set; }
    public int? ExperienceYears { get; set; }
    public string[] Specialties { get; set; } = [];
    public string[] Certifications { get; set; } = [];
    public string[] Languages { get; set; } = [];
    public string[] CoachingMethods { get; set; } = [];
    public string[] Achievements { get; set; } = [];
    public string? ClientResultsSummary { get; set; }
    public string? Availability { get; set; }
    public string? ResponseTime { get; set; }
    public string? CoachingStyle { get; set; }
    public decimal? MonthlyPriceAmount { get; set; }
    public decimal? SessionPriceAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Coach { get; set; } = null!;
}
