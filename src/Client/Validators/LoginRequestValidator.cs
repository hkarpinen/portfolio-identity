using Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
