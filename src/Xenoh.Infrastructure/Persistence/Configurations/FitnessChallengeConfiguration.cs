using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class FitnessChallengeConfiguration : IEntityTypeConfiguration<FitnessChallenge>
{
    public void Configure(EntityTypeBuilder<FitnessChallenge> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.CreatorId, x.Status });
        builder.HasIndex(x => new { x.StartsOn, x.EndsOn });
        builder.HasOne(x => x.Creator).WithMany(x => x.CreatedFitnessChallenges)
            .HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FitnessChallengeMemberConfiguration : IEntityTypeConfiguration<FitnessChallengeMember>
{
    public void Configure(EntityTypeBuilder<FitnessChallengeMember> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.ChallengeId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasOne(x => x.Challenge).WithMany(x => x.Members)
            .HasForeignKey(x => x.ChallengeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany(x => x.FitnessChallengeMemberships)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
