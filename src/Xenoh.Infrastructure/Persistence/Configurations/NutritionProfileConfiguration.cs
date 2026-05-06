using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class NutritionProfileConfiguration : IEntityTypeConfiguration<NutritionProfile>
{
    public void Configure(EntityTypeBuilder<NutritionProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ActivityLevel).HasConversion<int>();
        builder.Property(p => p.Goal).HasConversion<int>();
        builder.Property(p => p.TargetWeightKg).HasColumnType("decimal(6,2)");
        builder.Property(p => p.ProteinPerKg).HasColumnType("decimal(4,2)");
        builder.Property(p => p.FatPerKg).HasColumnType("decimal(4,2)");

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.User)
            .WithOne(u => u.NutritionProfile)
            .HasForeignKey<NutritionProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
