using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class AccountDeletionAuditLogConfiguration : IEntityTypeConfiguration<AccountDeletionAuditLog>
{
    public void Configure(EntityTypeBuilder<AccountDeletionAuditLog> builder)
    {
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(1000);
        builder.HasOne(x => x.AccountDeletionRequest)
            .WithMany()
            .HasForeignKey(x => x.AccountDeletionRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.AccountDeletionRequestId, x.CreatedAt });
    }
}
