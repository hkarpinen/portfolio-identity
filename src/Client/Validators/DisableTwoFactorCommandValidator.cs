using Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(x => x.CurrentCode).NotEmpty().Length(6, 8);
    }
}
