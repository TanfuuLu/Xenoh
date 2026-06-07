using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class AiUsageQuotaConfiguration : IEntityTypeConfiguration<AiUsageQuota>
{
    public void Configure(EntityTypeBuilder<AiUsageQuota> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.LastFeature).HasMaxLength(64);

        builder.HasIndex(q => new { q.UserId, q.PeriodStart }).IsUnique();

        builder.HasOne(q => q.User)
            .WithMany()
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
