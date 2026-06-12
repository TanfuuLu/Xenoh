using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SecondaryMuscleGroups)
            .HasColumnType("jsonb");
        builder.Property(e => e.ExerciseKind).HasDefaultValue(Xenoh.Domain.Enums.ExerciseKind.Strength);
        builder.Property(e => e.EstimatedMet).HasPrecision(5, 2).HasDefaultValue(5.0m);
        builder.Property(e => e.IsSkipped).HasDefaultValue(false);
        builder.Property(e => e.XpAwarded).HasDefaultValue(false);

        builder.HasIndex(e => new { e.DailyWorkoutId, e.SortOrder });
        builder.HasIndex(e => new { e.DailyWorkoutId, e.IsCompleted });
        builder.HasIndex(e => new { e.DailyWorkoutId, e.IsSkipped });
        builder.HasIndex(e => e.ExerciseTemplateId);

        builder.HasOne(e => e.ExerciseTemplate)
            .WithMany()
            .HasForeignKey(e => e.ExerciseTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
