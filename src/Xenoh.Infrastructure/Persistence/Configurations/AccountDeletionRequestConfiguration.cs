using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class AccountDeletionRequestConfiguration : IEntityTypeConfiguration<AccountDeletionRequest>
{
    public void Configure(EntityTypeBuilder<AccountDeletionRequest> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.VerificationTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Email, x.Status });
        builder.HasIndex(x => x.VerificationTokenHash).IsUnique();
    }
}
