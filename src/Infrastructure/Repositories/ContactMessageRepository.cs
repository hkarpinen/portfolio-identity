using MassTransit;
using Application.Repositories;
using Domain.Aggregates.Contact;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

internal sealed class ContactMessageRepository : IContactMessageRepository
{
    private readonly IdentityDbContext _dbContext;

    private readonly IPublishEndpoint _publishEndpoint;

    public ContactMessageRepository(IdentityDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task AddAsync(ContactMessage message, CancellationToken ct = default)
    {
        await _dbContext.ContactMessages.AddAsync(message, ct);

        foreach (var domainEvent in message.DomainEvents)
            await _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), ct);
        message.ClearDomainEvents();

        await _dbContext.SaveChangesAsync(ct);
    }
}
