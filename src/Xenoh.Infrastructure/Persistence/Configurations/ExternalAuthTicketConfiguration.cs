using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public class ExternalAuthTicketConfiguration : IEntityTypeConfiguration<ExternalAuthTicket>
{
    public void Configure(EntityTypeBuilder<ExternalAuthTicket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(t => t.TicketHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.UsedAt, t.ExpiresAt });

        builder.HasOne(t => t.User)
            .WithMany(u => u.ExternalAuthTickets)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
