using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class CoachClientRelationshipConfiguration : IEntityTypeConfiguration<CoachClientRelationship>
{
    public void Configure(EntityTypeBuilder<CoachClientRelationship> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Client)
            .WithOne(u => u.CoachRelationship)
            .HasForeignKey<CoachClientRelationship>(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Coach)
            .WithMany(u => u.Clients)
            .HasForeignKey(r => r.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ClientId)
            .IsUnique()
            .HasFilter("\"Status\" <> 2");

        builder.HasIndex(r => new { r.Status, r.EndDate });
    }
}
