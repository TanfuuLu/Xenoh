using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class DailyWorkoutConfiguration : IEntityTypeConfiguration<DailyWorkout>
{
    public void Configure(EntityTypeBuilder<DailyWorkout> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => new { d.WeeklyWorkoutId, d.Date });
        builder.HasIndex(d => new { d.WeeklyWorkoutId, d.Status });
    }
}
