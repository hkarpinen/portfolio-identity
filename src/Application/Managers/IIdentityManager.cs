using Application.Contracts;

namespace Application.Managers;

public interface IIdentityManager
{
    Task<Result> RegisterAsync(RegisterRequest request);
    Task<Result<LoginResult>> LoginAsync(LoginRequest request);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<Result<EnableTwoFactorResponse>> EnableTwoFactorAsync(Guid userId);
    Task<Result<LoginResult>> VerifyTwoFactorAsync(TwoFactorVerifyRequest request);
    Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<Result<UploadAvatarResponse>> UploadAvatarAsync(Guid userId, UploadAvatarRequest request);
    Task<Result> BanAsync(Guid userId);
    Task<Result> ChangeRoleAsync(Guid userId, string role);
}
