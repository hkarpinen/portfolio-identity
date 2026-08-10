using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "identity");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Every read is "this user's sessions" or "this lineage", so both get an index.
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.FamilyId);

        builder.Property(t => t.UserAgent).HasMaxLength(400);
        builder.Property(t => t.IpAddress).HasMaxLength(60);
    }
}
