using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.TransferCode).IsRequired().HasMaxLength(50);
        builder.Property(o => o.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.RequestedTier).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.SePayTransactionId).HasMaxLength(50);
        builder.Property(o => o.SePayReferenceCode).HasMaxLength(100);

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Subscription)
            .WithMany(s => s.PaymentOrders)
            .HasForeignKey(o => o.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.TransferCode).IsUnique();

        // Partial unique index: prevents duplicate SePay webhook processing, allows multiple NULLs
        builder.HasIndex(o => o.SePayTransactionId)
            .IsUnique()
            .HasFilter("\"SePayTransactionId\" IS NOT NULL");

        builder.HasIndex(o => new { o.UserId, o.Status });
    }
}
