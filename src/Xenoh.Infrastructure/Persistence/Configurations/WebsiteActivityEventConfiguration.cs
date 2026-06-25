using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class WebsiteActivityEventConfiguration : IEntityTypeConfiguration<WebsiteActivityEvent>
{
    public void Configure(EntityTypeBuilder<WebsiteActivityEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Path).IsRequired().HasMaxLength(500);
        builder.Property(e => e.PreviousPath).HasMaxLength(500);
        builder.Property(e => e.Referrer).HasMaxLength(1000);
        builder.Property(e => e.UtmSource).HasMaxLength(120);
        builder.Property(e => e.UtmMedium).HasMaxLength(120);
        builder.Property(e => e.UtmCampaign).HasMaxLength(200);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        builder.HasIndex(e => new { e.EventType, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.SessionId, e.OccurredAtUtc });
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.UtmSource);
        builder.HasIndex(e => e.Path);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
