using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class SupplementRegimenConfiguration : IEntityTypeConfiguration<SupplementRegimen>
{
    public void Configure(EntityTypeBuilder<SupplementRegimen> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(100);
        builder.Property(x => x.Form).HasMaxLength(50);
        builder.Property(x => x.Instructions).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.IsArchived });

        builder.HasOne(x => x.User)
            .WithMany(x => x.SupplementRegimens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedSupplementRegimens)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SupplementScheduleVersionConfiguration : IEntityTypeConfiguration<SupplementScheduleVersion>
{
    public void Configure(EntityTypeBuilder<SupplementScheduleVersion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RegimenId, x.EffectiveFrom, x.EffectiveTo });

        builder.HasOne(x => x.Regimen)
            .WithMany(x => x.ScheduleVersions)
            .HasForeignKey(x => x.RegimenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedSupplementScheduleVersions)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SupplementDoseSlotConfiguration : IEntityTypeConfiguration<SupplementDoseSlot>
{
    public void Configure(EntityTypeBuilder<SupplementDoseSlot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(10,3)");
        builder.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Weekdays).HasConversion<int>();
        builder.HasIndex(x => new { x.ScheduleVersionId, x.Time });

        builder.HasOne(x => x.ScheduleVersion)
            .WithMany(x => x.DoseSlots)
            .HasForeignKey(x => x.ScheduleVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupplementIntakeLogConfiguration : IEntityTypeConfiguration<SupplementIntakeLog>
{
    public void Configure(EntityTypeBuilder<SupplementIntakeLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.HasIndex(x => new { x.DoseSlotId, x.ScheduledDate }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ScheduledDate });

        builder.HasOne(x => x.User)
            .WithMany(x => x.SupplementIntakeLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.DoseSlot)
            .WithMany(x => x.IntakeLogs)
            .HasForeignKey(x => x.DoseSlotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
