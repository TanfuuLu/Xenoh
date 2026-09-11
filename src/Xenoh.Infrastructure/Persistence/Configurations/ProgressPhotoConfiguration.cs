using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class ProgressPhotoConfiguration : IEntityTypeConfiguration<ProgressPhoto>
{
    public void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(512);
        builder.HasIndex(x => new { x.ProgressCheckInId, x.SortOrder });
    }
}
