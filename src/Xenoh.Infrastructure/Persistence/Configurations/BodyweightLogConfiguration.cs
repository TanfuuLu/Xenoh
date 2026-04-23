using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class BodyweightLogConfiguration : IEntityTypeConfiguration<BodyweightLog>
{
    public void Configure(EntityTypeBuilder<BodyweightLog> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Weight).HasColumnType("decimal(6,2)");

        builder.HasIndex(b => new { b.UserId, b.Date }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
