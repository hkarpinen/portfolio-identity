using Application;
using Application.Dtos;

namespace Identity.Application.Managers.Contact;

public interface IContactManager
{
    Task<Result> SubmitAsync(ContactMessageDto message, CancellationToken cancellationToken = default);
}
