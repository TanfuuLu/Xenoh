using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.CodeHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.ResetToken)
            .IsRequired();

        builder.HasIndex(c => new { c.Email, c.UsedAt, c.ExpiresAt });

        builder.HasOne(c => c.User)
            .WithMany(u => u.PasswordResetCodes)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
