using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users", "identity");

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Handle)
            .HasMaxLength(40);

        // Filter uses the snake-cased column name produced by UseSnakeCaseNamingConvention()
        // — `"Handle"` (quoted, case-sensitive) would not resolve since the column is `handle`.
        builder.HasIndex(u => u.Handle).IsUnique().HasFilter("handle IS NOT NULL");

        builder.Property(u => u.Bio)
            .HasMaxLength(500);

        builder.Property(u => u.Location)
            .HasMaxLength(100);

        builder.Property(u => u.Pronouns)
            .HasMaxLength(40);

        builder.Property(u => u.TwoFactorEnabledAt);
        builder.Property(u => u.DeletedAt);
        builder.HasIndex(u => u.DeletedAt);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsDemo)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DemoExpiresAt);
        builder.Property(u => u.DemoExpiredAt);

        builder.Ignore(u => u.DomainEvents);
    }
}
