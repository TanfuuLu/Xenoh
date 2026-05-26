using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(2)
            .HasDefaultValue("vi");

        builder.Property(u => u.PreferredTheme)
            .HasMaxLength(5)
            .HasDefaultValue("light");

        builder.Property(u => u.PreferredWeightUnit)
            .HasMaxLength(2)
            .HasDefaultValue("kg");
    }
}
