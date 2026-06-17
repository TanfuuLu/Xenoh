using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(f => new { f.UserAId, f.UserBId }).IsUnique();
        builder.HasIndex(f => new { f.RequesterId, f.Status });
        builder.HasIndex(f => new { f.AddresseeId, f.Status });
        builder.HasIndex(f => new { f.UserAId, f.Status });
        builder.HasIndex(f => new { f.UserBId, f.Status });

        builder.HasOne(f => f.UserA)
            .WithMany(u => u.FriendshipsAsUserA)
            .HasForeignKey(f => f.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.UserB)
            .WithMany(u => u.FriendshipsAsUserB)
            .HasForeignKey(f => f.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Requester)
            .WithMany(u => u.FriendRequestsMade)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Addressee)
            .WithMany(u => u.FriendRequestsReceived)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
