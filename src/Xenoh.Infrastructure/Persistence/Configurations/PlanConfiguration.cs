using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PlanType).IsRequired();

        builder.HasOne(p => p.Owner)
            .WithMany(u => u.Plans)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CreatedByCoach)
            .WithMany()
            .HasForeignKey(p => p.CreatedByCoachId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(p => p.WeeklyWorkouts)
            .WithOne(w => w.Plan)
            .HasForeignKey(w => w.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
