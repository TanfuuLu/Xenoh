using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class PromotionCodeConfiguration : IEntityTypeConfiguration<PromotionCode>
{
    public void Configure(EntityTypeBuilder<PromotionCode> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(300);
        builder.Property(p => p.DiscountType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.DiscountValue).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.AppliesToTier).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasMany(p => p.PaymentOrders)
            .WithOne(o => o.PromotionCode)
            .HasForeignKey(o => o.PromotionCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
