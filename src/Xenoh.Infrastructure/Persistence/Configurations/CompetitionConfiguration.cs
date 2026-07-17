using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class OrganizerProfileConfiguration : IEntityTypeConfiguration<OrganizerProfile>
{
    public void Configure(EntityTypeBuilder<OrganizerProfile> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => x.Status);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.EvidenceFile).WithMany().HasForeignKey(x => x.EvidenceFileId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CompetitionEventConfiguration : IEntityTypeConfiguration<CompetitionEvent>
{
    public void Configure(EntityTypeBuilder<CompetitionEvent> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Status, x.StartsAtUtc });
        b.HasIndex(x => new { x.OwnerId, x.Status });
        b.Property(x => x.Discipline).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.PowerliftingScoringFormula).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.RegistrationFee).HasPrecision(14, 2);
        b.Property(x => x.Version).IsRowVersion();
        b.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompetitionEventStaffConfiguration : IEntityTypeConfiguration<CompetitionEventStaff>
{
    public void Configure(EntityTypeBuilder<CompetitionEventStaff> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
        b.Property(x => x.Permissions).HasConversion<int>();
        b.HasOne(x => x.Event).WithMany(x => x.Staff).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CompetitionCategoryConfiguration : IEntityTypeConfiguration<CompetitionCategory>
{
    public void Configure(EntityTypeBuilder<CompetitionCategory> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EventId, x.Code }).IsUnique();
        b.Property(x => x.MinAge).HasPrecision(5, 2);
        b.Property(x => x.MaxAge).HasPrecision(5, 2);
        b.Property(x => x.MinWeightKg).HasPrecision(7, 2);
        b.Property(x => x.MaxWeightKg).HasPrecision(7, 2);
        b.Property(x => x.MinHeightCm).HasPrecision(6, 2);
        b.Property(x => x.MaxHeightCm).HasPrecision(6, 2);
        b.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CompetitionRegistrationConfiguration : IEntityTypeConfiguration<CompetitionRegistration>
{
    public void Configure(EntityTypeBuilder<CompetitionRegistration> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique().HasFilter("\"UserId\" IS NOT NULL AND \"Status\" <> 'Withdrawn'");
        b.HasIndex(x => new { x.EventId, x.Status, x.SubmittedAt });
        b.HasIndex(x => new { x.CategoryId, x.Status });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.ExpectedFee).HasPrecision(14, 2);
        b.Property(x => x.DeclaredWeightKg).HasPrecision(7, 2);
        b.Property(x => x.DeclaredHeightCm).HasPrecision(6, 2);
        b.Ignore(x => x.IsConfirmed);
        b.HasOne(x => x.Event).WithMany(x => x.Registrations).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CompetitionPaymentReceiptConfiguration : IEntityTypeConfiguration<CompetitionPaymentReceipt>
{
    public void Configure(EntityTypeBuilder<CompetitionPaymentReceipt> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RegistrationId, x.CreatedAt });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Registration).WithMany(x => x.Receipts).HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PowerliftingCompetitionResultConfiguration : IEntityTypeConfiguration<PowerliftingCompetitionResult>
{
    public void Configure(EntityTypeBuilder<PowerliftingCompetitionResult> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.RegistrationId).IsUnique();
        b.Property(x => x.Formula).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
        foreach (var p in new[] { nameof(PowerliftingCompetitionResult.BodyweightKg), nameof(PowerliftingCompetitionResult.BestSquatKg), nameof(PowerliftingCompetitionResult.BestBenchKg), nameof(PowerliftingCompetitionResult.BestDeadliftKg), nameof(PowerliftingCompetitionResult.TotalKg), nameof(PowerliftingCompetitionResult.Score) })
            b.Property<decimal>(p).HasPrecision(10, 4);
        b.HasOne(x => x.Registration).WithOne(x => x.PowerliftingResult).HasForeignKey<PowerliftingCompetitionResult>(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BodybuildingCompetitionResultConfiguration : IEntityTypeConfiguration<BodybuildingCompetitionResult>
{
    public void Configure(EntityTypeBuilder<BodybuildingCompetitionResult> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.RegistrationId).IsUnique();
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(24);
        b.HasOne(x => x.Registration).WithOne(x => x.BodybuildingResult).HasForeignKey<BodybuildingCompetitionResult>(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CompetitionAuditLogConfiguration : IEntityTypeConfiguration<CompetitionAuditLog>
{
    public void Configure(EntityTypeBuilder<CompetitionAuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EventId, x.CreatedAt });
    }
}
