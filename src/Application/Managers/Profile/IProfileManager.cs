using Application;
using Application.Contracts;

namespace Identity.Application.Managers.Profile;

public interface IProfileManager
{
    Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<Result<UploadAvatarResponse>> UploadAvatarAsync(Guid userId, UploadAvatarRequest request);
}
