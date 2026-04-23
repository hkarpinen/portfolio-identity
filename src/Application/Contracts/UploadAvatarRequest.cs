namespace Application.Contracts;

public sealed record UploadAvatarRequest(Stream Content, string ContentType, long Length);

public sealed record UploadAvatarResponse(string AvatarUrl);
