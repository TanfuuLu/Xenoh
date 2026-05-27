using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class WeeklyWorkoutConfiguration : IEntityTypeConfiguration<WeeklyWorkout>
{
    public void Configure(EntityTypeBuilder<WeeklyWorkout> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => new { w.PlanId, w.WeekNumber });
        builder.HasIndex(w => new { w.PlanId, w.StartDate, w.EndDate });
    }
}
