using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Every token refresh looks up the row by Token (FirstOrDefaultAsync(r => r.Token == token)).
        // Without this index that lookup is a sequential scan that grows with every login.
        // Tokens are cryptographically unique, so a unique index is both correct and optimal.
        builder.HasIndex(r => r.Token).IsUnique();
    }
}
