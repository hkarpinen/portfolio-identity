using Application;
using Application.Dtos;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.Contact;

namespace Identity.Application.Managers.Contact;

internal sealed class ContactManager : IContactManager
{
    private readonly IRecaptchaService _recaptcha;
    private readonly IContactMessageRepository _repository;

    public ContactManager(IRecaptchaService recaptcha, IContactMessageRepository repository)
    {
        _recaptcha = recaptcha;
        _repository = repository;
    }

    public async Task<Result> SubmitAsync(ContactMessageDto message, CancellationToken cancellationToken = default)
    {
        if (!await _recaptcha.VerifyAsync(message.CaptchaToken, "contact", cancellationToken))
            return Result.Failure("CAPTCHA verification failed. Please try again.");

        var contactMessage = ContactMessage.Submit(
            message.Name, message.Email, message.Subject, message.Message);

        await _repository.AddAsync(contactMessage, cancellationToken);
        return Result.Success();
    }
}
