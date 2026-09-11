using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class ProgressCheckInConfiguration : IEntityTypeConfiguration<ProgressCheckIn>
{
    public void Configure(EntityTypeBuilder<ProgressCheckIn> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(2_000);
        builder.Property(x => x.BodyweightKg).HasPrecision(5, 2);
        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Photos).WithOne(x => x.ProgressCheckIn).HasForeignKey(x => x.ProgressCheckInId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.OwnerId, x.CheckInDate });
        builder.HasIndex(x => new { x.OwnerId, x.IsSharedWithCoach });
    }
}
