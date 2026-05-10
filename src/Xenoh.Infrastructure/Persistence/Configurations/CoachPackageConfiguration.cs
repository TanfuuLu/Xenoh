using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class CoachPackageConfiguration : IEntityTypeConfiguration<CoachPackage>
{
    public void Configure(EntityTypeBuilder<CoachPackage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(80).IsRequired();
        builder.Property(p => p.PriceAmount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(8).IsRequired();
        builder.Property(p => p.DurationLabel).HasMaxLength(80).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.Property(p => p.Type).HasMaxLength(80);

        builder.HasIndex(p => new { p.CoachMarketplaceProfileId, p.DisplayOrder });
    }
}
