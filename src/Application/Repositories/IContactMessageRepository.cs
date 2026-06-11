using Domain.Aggregates.Contact;

namespace Application.Repositories;

public interface IContactMessageRepository
{
    Task AddAsync(ContactMessage message, CancellationToken ct = default);
}
