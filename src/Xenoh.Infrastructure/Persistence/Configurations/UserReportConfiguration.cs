using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Details).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.AdminNote).HasMaxLength(2000);
        builder.Property(r => r.RelatedEntityType).HasMaxLength(50);

        builder.HasIndex(r => new { r.Status, r.CreatedAt });
        builder.HasIndex(r => r.ReportedUserId);

        builder.HasOne(r => r.Reporter)
            .WithMany(u => u.ReportsMade)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedUser)
            .WithMany(u => u.ReportsReceived)
            .HasForeignKey(r => r.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany(u => u.ReportsReviewed)
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
