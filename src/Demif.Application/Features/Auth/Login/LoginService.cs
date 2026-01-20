using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Auth.Login;

/// <summary>
/// Login Service - xử lý logic đăng nhập
/// (Giống Service trong 3-layer architecture)
/// </summary>
public class LoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Tìm user theo email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Invalid email or password."));
        }

        // 2. Verify password
        if (user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Invalid email or password."));
        }

        // 3. Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // 4. Return response
        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Match token expiration
        };
    }
}
