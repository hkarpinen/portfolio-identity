using Application.Dtos;
using FluentValidation;

namespace Client.Validators;

public sealed class ContactMessageDtoValidator : AbstractValidator<ContactMessageDto>
{
    public ContactMessageDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.CaptchaToken).NotEmpty();
    }
}
