using Domain.Aggregates.Contact;
using Domain.Aggregates.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class IdentityDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
