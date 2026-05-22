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
            .Must(url =>
            {
                if (url is null) return true;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
                return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
            })
            .WithMessage("AvatarUrl must be an absolute http or https URL.")
            .When(x => x.AvatarUrl is not null);
    }
}
