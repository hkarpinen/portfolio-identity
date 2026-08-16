using Domain.Aggregates.Contact;
using Domain.Aggregates.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class IdentityDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // MassTransit's transactional outbox and inbox, replacing the hand-rolled outbox_messages
        // table and its polling publisher.
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
        builder.HasDefaultSchema("identity");
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
