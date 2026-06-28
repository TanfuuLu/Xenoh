using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(f => f.StorageKey)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(f => f.SizeBytes).IsRequired();

        builder.HasOne(f => f.Owner)
            .WithMany()
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Shares)
            .WithOne(s => s.File)
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.OwnerId);
    }
}
