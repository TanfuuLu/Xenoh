using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class UserExercisePRHistoryConfiguration : IEntityTypeConfiguration<UserExercisePRHistory>
{
    public void Configure(EntityTypeBuilder<UserExercisePRHistory> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Weight).HasColumnType("decimal(10,2)");

        builder.HasIndex(p => new { p.UserId, p.ExerciseTemplateId, p.AchievedAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ExerciseTemplate>()
            .WithMany()
            .HasForeignKey(p => p.ExerciseTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
