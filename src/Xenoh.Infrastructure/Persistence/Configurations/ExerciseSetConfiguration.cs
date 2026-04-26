using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class ExerciseSetConfiguration : IEntityTypeConfiguration<ExerciseSet>
{
    public void Configure(EntityTypeBuilder<ExerciseSet> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PlannedWeight).HasColumnType("decimal(10,2)");
        builder.Property(s => s.ActualWeight).HasColumnType("decimal(10,2)");
        builder.Property(s => s.Rpe).HasColumnType("decimal(4,2)");

        builder.HasOne(s => s.Exercise)
            .WithMany(e => e.Sets)
            .HasForeignKey(s => s.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
