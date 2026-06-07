using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class AiFeatureUsageConfiguration : IEntityTypeConfiguration<AiFeatureUsage>
{
    public void Configure(EntityTypeBuilder<AiFeatureUsage> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Feature)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(u => new { u.UserId, u.PeriodStart, u.Feature }).IsUnique();
        builder.HasIndex(u => new { u.PeriodStart, u.Feature });

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
