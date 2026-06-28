using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class StoredFileShareConfiguration : IEntityTypeConfiguration<StoredFileShare>
{
    public void Configure(EntityTypeBuilder<StoredFileShare> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SharedByUserId).IsRequired();
        builder.Property(s => s.SharedWithUserId).IsRequired();

        builder.HasOne(s => s.SharedWithUser)
            .WithMany()
            .HasForeignKey(s => s.SharedWithUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A file can be shared with a given user at most once.
        builder.HasIndex(s => new { s.FileId, s.SharedWithUserId }).IsUnique();
        builder.HasIndex(s => s.SharedWithUserId);
    }
}
