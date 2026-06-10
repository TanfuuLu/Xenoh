using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class CycleDailyLogConfiguration : IEntityTypeConfiguration<CycleDailyLog>
{
    public void Configure(EntityTypeBuilder<CycleDailyLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasIndex(l => new { l.UserId, l.Date }).IsUnique();

        builder.HasOne(l => l.User)
            .WithMany(u => u.CycleDailyLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
