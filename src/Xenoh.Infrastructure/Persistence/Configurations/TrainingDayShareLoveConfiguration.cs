using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class TrainingDayShareLoveConfiguration : IEntityTypeConfiguration<TrainingDayShareLove>
{
    public void Configure(EntityTypeBuilder<TrainingDayShareLove> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.TrainingDayShareId, l.UserId }).IsUnique();
        builder.HasIndex(l => l.UserId);

        builder.HasOne(l => l.TrainingDayShare)
            .WithMany(s => s.Loves)
            .HasForeignKey(l => l.TrainingDayShareId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany(u => u.TrainingDayShareLoves)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
