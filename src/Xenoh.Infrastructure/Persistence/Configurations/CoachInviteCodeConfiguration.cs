using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class CoachInviteCodeConfiguration : IEntityTypeConfiguration<CoachInviteCode>
{
    public void Configure(EntityTypeBuilder<CoachInviteCode> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(8)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.HasOne(c => c.Coach)
            .WithMany()
            .HasForeignKey(c => c.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
