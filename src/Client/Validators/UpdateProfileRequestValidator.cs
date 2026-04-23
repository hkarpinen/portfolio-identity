using Application.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("AvatarUrl must be a valid URL.")
            .When(x => x.AvatarUrl is not null);
    }
}
