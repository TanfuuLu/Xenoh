using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class MealPlanItemConfiguration : IEntityTypeConfiguration<MealPlanItem>
{
    public void Configure(EntityTypeBuilder<MealPlanItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Grams).HasColumnType("decimal(8,2)");
        builder.Property(i => i.ServingLabelVi).HasMaxLength(100);
        builder.Property(i => i.ServingLabelEn).HasMaxLength(100);
        builder.Property(i => i.ServingCount).HasColumnType("decimal(6,2)");
        builder.Property(i => i.PlannedProteinG).HasColumnType("decimal(7,2)");
        builder.Property(i => i.PlannedCarbsG).HasColumnType("decimal(7,2)");
        builder.Property(i => i.PlannedFatG).HasColumnType("decimal(7,2)");

        builder.HasIndex(i => new { i.MealPlanMealId, i.SortOrder });
        builder.HasIndex(i => i.FoodLogId).IsUnique();

        builder.HasOne(i => i.FoodItem)
            .WithMany()
            .HasForeignKey(i => i.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CheckedByUser)
            .WithMany()
            .HasForeignKey(i => i.CheckedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.FoodLog)
            .WithOne()
            .HasForeignKey<MealPlanItem>(i => i.FoodLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
