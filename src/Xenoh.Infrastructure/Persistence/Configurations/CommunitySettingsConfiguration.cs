using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class CommunitySettingsConfiguration : IEntityTypeConfiguration<CommunitySettings>
{
    public void Configure(EntityTypeBuilder<CommunitySettings> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.StatsVisibility).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.User).WithOne(x => x.CommunitySettings)
            .HasForeignKey<CommunitySettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
