using Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .Must(url => url is null || Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            .WithMessage("AvatarUrl must be a valid URL.")
            .When(x => x.AvatarUrl is not null);
    }
}
