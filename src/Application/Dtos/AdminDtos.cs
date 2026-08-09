namespace Application.Dtos;

public sealed record ChangeRoleDto(string Role);

/// <summary>
/// The reason an admin gives when locking an account out. Optional — a ban with no explanation is
/// still a valid ban, so an absent reason renders the date on its own rather than blocking the
/// action.
/// </summary>
public sealed record BanUserDto(string? Reason);
