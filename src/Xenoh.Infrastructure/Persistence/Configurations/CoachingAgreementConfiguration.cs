using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class CoachingAgreementConfiguration : IEntityTypeConfiguration<CoachingAgreement>
{
    public void Configure(EntityTypeBuilder<CoachingAgreement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(120);
        builder.Property(x => x.Goals).HasMaxLength(2000);
        builder.Property(x => x.Services).HasMaxLength(2000);
        builder.Property(x => x.CheckInFrequency).HasMaxLength(300);
        builder.Property(x => x.Availability).HasMaxLength(500);
        builder.Property(x => x.CoachResponsibilities).HasMaxLength(2000);
        builder.Property(x => x.ClientResponsibilities).HasMaxLength(2000);
        builder.Property(x => x.PolicyVersion).HasMaxLength(40);
        builder.Property(x => x.TimeZone).HasMaxLength(60);
        builder.Property(x => x.AcceptedAtUtc).IsConcurrencyToken();
        builder.Property(x => x.RejectedAtUtc).IsConcurrencyToken();
        builder.HasIndex(x => new { x.RelationshipId, x.Version }).IsUnique();
        builder.HasOne<CoachClientRelationship>().WithMany().HasForeignKey(x => x.RelationshipId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CoachingAgreementEventConfiguration : IEntityTypeConfiguration<CoachingAgreementEvent>
{
    public void Configure(EntityTypeBuilder<CoachingAgreementEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasMaxLength(60);
        builder.HasIndex(x => new { x.RelationshipId, x.OccurredAtUtc });
        builder.HasOne<CoachClientRelationship>().WithMany().HasForeignKey(x => x.RelationshipId).OnDelete(DeleteBehavior.Restrict);
    }
}
