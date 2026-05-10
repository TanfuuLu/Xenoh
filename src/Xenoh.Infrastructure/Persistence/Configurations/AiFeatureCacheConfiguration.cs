using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class AiFeatureCacheConfiguration : IEntityTypeConfiguration<AiFeatureCache>
{
    public void Configure(EntityTypeBuilder<AiFeatureCache> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Feature).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Language).HasMaxLength(8).IsRequired();
        builder.Property(c => c.DataFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(c => c.ContentJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(c => new { c.Feature, c.Language, c.UserId, c.SubjectUserId, c.ResourceId })
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.SubjectUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
