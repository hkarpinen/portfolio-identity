using Application;
using Application.Commands;
using Application.Dtos;

namespace Identity.Application.Managers.Profile;

public interface IProfileManager
{
    Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileCommand command);
    Task<Result<UploadAvatarDto>> UploadAvatarAsync(Guid userId, UploadAvatarCommand command);
    Task<Result> ChangeEmailAsync(Guid userId, ChangeEmailCommand command);
    Task<Result> DeleteAccountAsync(Guid userId, DeleteAccountCommand command);
    Task<Result<ConnectionsResponseDto>> GetConnectionsAsync(Guid userId);
    Task<Result> DisconnectOAuthAsync(Guid userId, DisconnectOAuthCommand command);
}
