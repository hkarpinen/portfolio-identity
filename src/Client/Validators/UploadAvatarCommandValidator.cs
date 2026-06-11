using Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    ];

    private const long MaxBytes = 5 * 1024 * 1024;

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Avatar must be PNG, JPEG, WebP, or GIF.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxBytes)
            .WithMessage("Avatar must be no larger than 5 MB.");
    }
}
