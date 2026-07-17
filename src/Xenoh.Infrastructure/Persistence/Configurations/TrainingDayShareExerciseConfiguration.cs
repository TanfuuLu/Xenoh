using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class TrainingDayShareExerciseConfiguration : IEntityTypeConfiguration<TrainingDayShareExercise>
{
    public void Configure(EntityTypeBuilder<TrainingDayShareExercise> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ExerciseKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.PrimaryMuscleGroup).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.SecondaryMuscleGroups).HasColumnType("jsonb");
        builder.Property(e => e.EstimatedMet).HasPrecision(5, 2);
        builder.Property(e => e.PlannedWeight).HasColumnType("decimal(10,2)");
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => new { e.TrainingDayShareId, e.SortOrder });
        builder.HasIndex(e => e.ExerciseTemplateId);

        builder.HasMany(e => e.Sets)
            .WithOne(s => s.TrainingDayShareExercise)
            .HasForeignKey(s => s.TrainingDayShareExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
