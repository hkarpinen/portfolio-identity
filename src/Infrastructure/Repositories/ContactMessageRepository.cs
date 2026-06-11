using Application.Repositories;
using Domain.Aggregates.Contact;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

internal sealed class ContactMessageRepository : IContactMessageRepository
{
    private readonly IdentityDbContext _dbContext;

    public ContactMessageRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(ContactMessage message, CancellationToken ct = default)
    {
        await _dbContext.ContactMessages.AddAsync(message, ct);

        foreach (var domainEvent in message.DomainEvents)
            _dbContext.AddToOutbox(domainEvent);
        message.ClearDomainEvents();

        await _dbContext.SaveChangesAsync(ct);
    }
}
