using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class FoodServingConfiguration : IEntityTypeConfiguration<FoodServing>
{
    public void Configure(EntityTypeBuilder<FoodServing> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Label).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Grams).HasColumnType("decimal(7,2)");

        builder.HasIndex(s => new { s.FoodItemId, s.Label }).IsUnique();
    }
}
