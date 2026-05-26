using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Kind)
            .HasConversion<string>()
            .HasMaxLength(24)
            .HasDefaultValue(Xenoh.Domain.Enums.MessageKind.User);

        builder.HasOne(m => m.Relationship)
            .WithMany()
            .HasForeignKey(m => m.RelationshipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.RelationshipId, m.CreatedAt });
        builder.HasIndex(m => new { m.RelationshipId, m.SenderId, m.IsRead });
    }
}
