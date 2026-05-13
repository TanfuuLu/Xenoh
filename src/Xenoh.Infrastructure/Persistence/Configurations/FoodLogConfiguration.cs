using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class FoodLogConfiguration : IEntityTypeConfiguration<FoodLog>
{
    public void Configure(EntityTypeBuilder<FoodLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Grams).HasColumnType("decimal(8,2)");
        builder.Property(l => l.ServingLabel).HasMaxLength(100);
        builder.Property(l => l.ServingCount).HasColumnType("decimal(6,2)");
        builder.Property(l => l.ComputedProteinG).HasColumnType("decimal(7,2)");
        builder.Property(l => l.ComputedCarbsG).HasColumnType("decimal(7,2)");
        builder.Property(l => l.ComputedFatG).HasColumnType("decimal(7,2)");

        builder.HasIndex(l => new { l.UserId, l.Date });

        builder.HasOne(l => l.User)
            .WithMany(u => u.FoodLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.FoodItem)
            .WithMany(f => f.FoodLogs)
            .HasForeignKey(l => l.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
