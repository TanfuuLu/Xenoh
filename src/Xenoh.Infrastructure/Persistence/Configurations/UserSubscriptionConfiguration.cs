using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Tier)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.ExpiresAt).IsRequired(false);

        builder.Property(s => s.ExpiryReminderSentAt).IsRequired(false);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Subscription)
            .HasForeignKey<UserSubscription>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId).IsUnique();

        builder.Ignore(s => s.IsActive);
    }
}
