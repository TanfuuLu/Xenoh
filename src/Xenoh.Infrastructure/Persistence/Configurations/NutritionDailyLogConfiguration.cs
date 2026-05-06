using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class NutritionDailyLogConfiguration : IEntityTypeConfiguration<NutritionDailyLog>
{
    public void Configure(EntityTypeBuilder<NutritionDailyLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProteinG).HasColumnType("decimal(7,2)");
        builder.Property(l => l.CarbsG).HasColumnType("decimal(7,2)");
        builder.Property(l => l.FatG).HasColumnType("decimal(7,2)");
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasIndex(l => new { l.UserId, l.Date }).IsUnique();

        builder.HasOne(l => l.User)
            .WithMany(u => u.NutritionDailyLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
