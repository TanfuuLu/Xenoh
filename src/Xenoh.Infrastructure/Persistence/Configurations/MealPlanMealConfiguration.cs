using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class MealPlanMealConfiguration : IEntityTypeConfiguration<MealPlanMeal>
{
    public void Configure(EntityTypeBuilder<MealPlanMeal> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(m => new { m.MealPlanDayId, m.SortOrder });

        builder.HasMany(m => m.Items)
            .WithOne(i => i.MealPlanMeal)
            .HasForeignKey(i => i.MealPlanMealId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
