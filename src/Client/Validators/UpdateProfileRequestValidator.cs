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

        RuleFor(x => x.Handle)
            .MaximumLength(40)
            .Matches("^[a-zA-Z0-9_]+$")
            .When(x => !string.IsNullOrEmpty(x.Handle))
            .WithMessage("Handle may only contain letters, numbers, and underscores.");

        RuleFor(x => x.Bio).MaximumLength(500).When(x => x.Bio is not null);
        RuleFor(x => x.Location).MaximumLength(100).When(x => x.Location is not null);
        RuleFor(x => x.Pronouns).MaximumLength(40).When(x => x.Pronouns is not null);
    }
}

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
