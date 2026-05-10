namespace Xenoh.Domain.Entities;

public class CoachPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CoachMarketplaceProfileId { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal? PriceAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string DurationLabel { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int DisplayOrder { get; set; }

    public CoachMarketplaceProfile CoachMarketplaceProfile { get; set; } = null!;
}
