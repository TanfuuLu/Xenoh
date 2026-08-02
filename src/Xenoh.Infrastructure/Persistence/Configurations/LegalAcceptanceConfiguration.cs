using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class LegalAcceptanceConfiguration : IEntityTypeConfiguration<LegalAcceptance>
{
    public void Configure(EntityTypeBuilder<LegalAcceptance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.DocumentVersion).IsRequired().HasMaxLength(40);
        builder.HasIndex(x => x.PaymentOrderId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.DocumentType, x.AcceptedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentOrder>()
            .WithOne()
            .HasForeignKey<LegalAcceptance>(x => x.PaymentOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
