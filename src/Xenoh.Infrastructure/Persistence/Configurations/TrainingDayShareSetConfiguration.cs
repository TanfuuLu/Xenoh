using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class TrainingDayShareSetConfiguration : IEntityTypeConfiguration<TrainingDayShareSet>
{
    public void Configure(EntityTypeBuilder<TrainingDayShareSet> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ActualWeight).HasColumnType("decimal(10,2)");
        builder.Property(s => s.PlannedWeight).HasColumnType("decimal(10,2)");
        builder.Property(s => s.Rpe).HasColumnType("decimal(4,2)");
        builder.HasIndex(s => new { s.TrainingDayShareExerciseId, s.SetNumber });
    }
}
