using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class CoachMarketplaceProfileConfiguration : IEntityTypeConfiguration<CoachMarketplaceProfile>
{
    public void Configure(EntityTypeBuilder<CoachMarketplaceProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Headline).HasMaxLength(120);
        builder.Property(p => p.ExperienceYears);
        builder.Property(p => p.Specialties).HasColumnType("text[]");
        builder.Property(p => p.Certifications).HasColumnType("text[]");
        builder.Property(p => p.Languages).HasColumnType("text[]");
        builder.Property(p => p.CoachingMethods).HasColumnType("text[]");
        builder.Property(p => p.Achievements).HasColumnType("text[]");
        builder.Property(p => p.ClientResultsSummary).HasMaxLength(500);
        builder.Property(p => p.Availability).HasMaxLength(500);
        builder.Property(p => p.ResponseTime).HasMaxLength(500);
        builder.Property(p => p.CoachingStyle).HasMaxLength(500);
        builder.Property(p => p.MonthlyPriceAmount).HasPrecision(18, 2);
        builder.Property(p => p.SessionPriceAmount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(8).IsRequired();

        builder.HasIndex(p => p.CoachId).IsUnique();

        builder.HasOne(p => p.Coach)
            .WithOne(u => u.CoachMarketplaceProfile)
            .HasForeignKey<CoachMarketplaceProfile>(p => p.CoachId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
