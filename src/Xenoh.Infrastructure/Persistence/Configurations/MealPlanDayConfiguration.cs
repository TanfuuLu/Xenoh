using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay>
{
    public void Configure(EntityTypeBuilder<MealPlanDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasIndex(d => new { d.UserId, d.Date }).IsUnique();

        builder.HasOne(d => d.User)
            .WithMany(u => u.MealPlanDays)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Meals)
            .WithOne(m => m.MealPlanDay)
            .HasForeignKey(m => m.MealPlanDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
