using Application.Dtos;
using FluentValidation;

namespace Client.Validators;

public sealed class ChangeRoleDtoValidator : AbstractValidator<ChangeRoleDto>
{
    private static readonly string[] AllowedRoles = ["User", "Mod", "Admin"];

    public ChangeRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be one of: User, Mod, Admin.");
    }
}
