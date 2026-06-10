using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class CycleSettingsConfiguration : IEntityTypeConfiguration<CycleSettings>
{
    public void Configure(EntityTypeBuilder<CycleSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.UserId).IsUnique();

        builder.HasOne(s => s.User)
            .WithOne(u => u.CycleSettings)
            .HasForeignKey<CycleSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
