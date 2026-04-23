using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class UserExercisePRConfiguration : IEntityTypeConfiguration<UserExercisePR>
{
    public void Configure(EntityTypeBuilder<UserExercisePR> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Weight).HasColumnType("decimal(10,2)");

        builder.HasIndex(p => new { p.UserId, p.ExerciseTemplateId }).IsUnique();

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
