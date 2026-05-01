using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class ExerciseTemplateConfiguration : IEntityTypeConfiguration<ExerciseTemplate>
{
    public void Configure(EntityTypeBuilder<ExerciseTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.SecondaryMuscleGroups).HasColumnType("jsonb");
        builder.Property(t => t.IsArchived).HasDefaultValue(false);
        builder.Property(t => t.ExerciseKind).HasDefaultValue(Xenoh.Domain.Enums.ExerciseKind.Strength);
        builder.Property(t => t.EstimatedMet).HasPrecision(5, 2).HasDefaultValue(5.0m);
        builder.Property(t => t.IsCompetitionLift).HasDefaultValue(false);
        builder.Property(t => t.CompetitionLiftType).IsRequired(false);
        builder.HasIndex(t => new { t.OwnerId, t.IsArchived });
        builder.HasOne(t => t.Owner)
            .WithMany(u => u.CustomExerciseTemplates)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
