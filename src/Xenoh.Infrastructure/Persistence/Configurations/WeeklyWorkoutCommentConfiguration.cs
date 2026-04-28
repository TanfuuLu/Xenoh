using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class WeeklyWorkoutCommentConfiguration : IEntityTypeConfiguration<WeeklyWorkoutComment>
{
    public void Configure(EntityTypeBuilder<WeeklyWorkoutComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired().HasMaxLength(1000);

        builder.HasOne(c => c.WeeklyWorkout)
            .WithMany(w => w.Comments)
            .HasForeignKey(c => c.WeeklyWorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.WeeklyWorkoutId, c.CreatedAt });
    }
}
