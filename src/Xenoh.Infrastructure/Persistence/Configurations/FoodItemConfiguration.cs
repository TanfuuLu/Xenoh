using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.NameVi).HasMaxLength(200).IsRequired();
        builder.Property(f => f.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(f => f.CaloriesPer100g).HasColumnType("decimal(7,2)");
        builder.Property(f => f.ProteinPer100g).HasColumnType("decimal(7,2)");
        builder.Property(f => f.CarbsPer100g).HasColumnType("decimal(7,2)");
        builder.Property(f => f.FatPer100g).HasColumnType("decimal(7,2)");

        builder.HasIndex(f => f.NameVi);
        builder.HasIndex(f => f.NameEn);

        builder.HasOne(f => f.CreatedByUser)
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(f => f.Servings)
            .WithOne(s => s.FoodItem)
            .HasForeignKey(s => s.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
