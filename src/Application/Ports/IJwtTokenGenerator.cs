using Domain.Aggregates.User;

namespace Application.Ports;

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(AppUser user);
}
