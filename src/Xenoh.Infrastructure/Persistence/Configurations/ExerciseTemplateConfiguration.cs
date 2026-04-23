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
        builder.Property(t => t.IsCompetitionLift).HasDefaultValue(false);
        builder.Property(t => t.CompetitionLiftType).IsRequired(false);
    }
}
