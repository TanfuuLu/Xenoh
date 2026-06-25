using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action).IsRequired().HasMaxLength(100);
        builder.Property(log => log.TargetType).IsRequired().HasMaxLength(100);
        builder.Property(log => log.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(log => log.BeforeSummary).IsRequired().HasMaxLength(2000);
        builder.Property(log => log.AfterSummary).IsRequired().HasMaxLength(2000);

        builder.HasIndex(log => new { log.Action, log.CreatedAt });
        builder.HasIndex(log => log.AdminUserId);
        builder.HasIndex(log => log.TargetUserId);
        builder.HasIndex(log => new { log.TargetType, log.TargetId });

        builder.HasOne(log => log.AdminUser)
            .WithMany()
            .HasForeignKey(log => log.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(log => log.TargetUser)
            .WithMany()
            .HasForeignKey(log => log.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
