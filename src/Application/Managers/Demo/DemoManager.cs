using Application;
using Application.Dtos;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.User;

namespace Identity.Application.Managers.Demo;

internal sealed class DemoManager : IDemoManager
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRecaptchaService _recaptcha;

    public DemoManager(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRecaptchaService recaptcha)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _recaptcha = recaptcha;
    }

    public async Task<Result<DemoStartDto>> StartDemoAsync(string captchaToken, CancellationToken cancellationToken = default)
    {
        if (!await _recaptcha.VerifyAsync(captchaToken, "demo_start", cancellationToken))
            return Result<DemoStartDto>.Failure("CAPTCHA verification failed. Please try again.");

        var user = AppUser.CreateDemo("Demo User");

        var (succeeded, error) = await _userRepository.CreateDemoAsync(user, cancellationToken);
        if (!succeeded)
            return Result<DemoStartDto>.Failure(error!);

        var tokenResult = _jwtTokenGenerator.GenerateToken(
            user,
            overrideExpiry: new DateTimeOffset(user.DemoExpiresAt!.Value, TimeSpan.Zero));

        return Result<DemoStartDto>.Success(new DemoStartDto(tokenResult.Token, tokenResult.ExpiresAt.UtcDateTime));
    }
}
