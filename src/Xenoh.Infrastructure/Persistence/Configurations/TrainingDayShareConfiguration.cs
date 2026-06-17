using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class TrainingDayShareConfiguration : IEntityTypeConfiguration<TrainingDayShare>
{
    public void Configure(EntityTypeBuilder<TrainingDayShare> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DayStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Caption).HasMaxLength(500);
        builder.Property(s => s.TotalVolume).HasColumnType("decimal(12,2)");
        builder.Property(s => s.AverageRpe).HasColumnType("decimal(4,2)");

        builder.HasIndex(s => new { s.UserId, s.CreatedAt });
        builder.HasIndex(s => s.SourceDailyWorkoutId);

        builder.HasOne(s => s.User)
            .WithMany(u => u.TrainingDayShares)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Exercises)
            .WithOne(e => e.TrainingDayShare)
            .HasForeignKey(e => e.TrainingDayShareId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Loves)
            .WithOne(l => l.TrainingDayShare)
            .HasForeignKey(l => l.TrainingDayShareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
