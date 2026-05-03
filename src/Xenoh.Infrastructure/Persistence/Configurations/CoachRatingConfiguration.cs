using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class CoachRatingConfiguration : IEntityTypeConfiguration<CoachRating>
{
    public void Configure(EntityTypeBuilder<CoachRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);

        builder.HasIndex(r => new { r.CoachId, r.ClientId }).IsUnique();

        builder.HasOne(r => r.Coach)
            .WithMany(u => u.CoachRatingsReceived)
            .HasForeignKey(r => r.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Client)
            .WithMany(u => u.CoachRatingsGiven)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
