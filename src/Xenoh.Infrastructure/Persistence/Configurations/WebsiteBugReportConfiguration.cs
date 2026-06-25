using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class WebsiteBugReportConfiguration : IEntityTypeConfiguration<WebsiteBugReport>
{
    public void Configure(EntityTypeBuilder<WebsiteBugReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).IsRequired().HasMaxLength(160);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(3000);
        builder.Property(r => r.PageUrl).HasMaxLength(1000);
        builder.Property(r => r.BrowserInfo).HasMaxLength(500);
        builder.Property(r => r.AdminNote).HasMaxLength(2000);

        builder.Property(r => r.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(r => new { r.Status, r.CreatedAt });
        builder.HasIndex(r => r.Severity);
        builder.HasIndex(r => r.UserId);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
