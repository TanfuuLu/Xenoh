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
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.MetricType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.AccessType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.SelectedLifts).HasColumnType("jsonb");
        builder.Property(x => x.CheckInPrompt).HasMaxLength(160);
        builder.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.CreatorId, x.Status });
        builder.HasIndex(x => new { x.AccessType, x.StartsAtUtc, x.EndsAtUtc });
        builder.HasOne(x => x.Creator).WithMany(x => x.CreatedFitnessChallenges)
            .HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FitnessChallengeCheckInConfiguration : IEntityTypeConfiguration<FitnessChallengeCheckIn>
{
    public void Configure(EntityTypeBuilder<FitnessChallengeCheckIn> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.ChallengeId, x.UserId, x.LocalDate }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.LocalDate });
        builder.HasOne(x => x.Challenge).WithMany(x => x.CheckIns)
            .HasForeignKey(x => x.ChallengeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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
