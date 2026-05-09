using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class UserAnalysisConfiguration : IEntityTypeConfiguration<UserAnalysis>
{
    public void Configure(EntityTypeBuilder<UserAnalysis> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Language).HasMaxLength(8).IsRequired();
        builder.Property(a => a.DataFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(a => a.ContentJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(a => new { a.UserId, a.Language }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
