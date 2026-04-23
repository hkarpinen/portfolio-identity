using Application.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(50);
    }
}
