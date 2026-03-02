using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.GoogleLogin;
using Demif.Application.Features.Auth.Register;
using Demif.Application.Features.Auth.RefreshToken;
using Demif.Application.Features.Auth.Logout;
using Demif.Application.Features.Auth.VerifyEmail;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Auth Controller — Email/Password + Google OAuth + Email Verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginService _loginService;
    private readonly GoogleLoginService _googleLoginService;
    private readonly RegisterService _registerService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly LogoutService _logoutService;
    private readonly VerifyEmailService _verifyEmailService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthController(
        LoginService loginService,
        GoogleLoginService googleLoginService,
        RegisterService registerService,
        RefreshTokenService refreshTokenService,
        LogoutService logoutService,
        VerifyEmailService verifyEmailService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator)
    {
        _loginService = loginService;
        _googleLoginService = googleLoginService;
        _registerService = registerService;
        _refreshTokenService = refreshTokenService;
        _logoutService = logoutService;
        _verifyEmailService = verifyEmailService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    /// <summary>
    /// Đăng ký tài khoản mới — gửi email xác nhận, KHÔNG trả JWT ngay.
    /// Fields: email, password, confirmPassword, username, nativeLanguage, targetLanguage, country
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _registerService.ExecuteAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Xác nhận email sau khi đăng ký — trả về JWT để auto-login.
    /// GET /api/auth/verify-email?token=xxx
    /// </summary>
    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Token không được để trống." });

        var result = await _verifyEmailService.ExecuteAsync(token, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                "Auth.TokenExpired" => BadRequest(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng nhập Email/Password — yêu cầu email đã xác nhận.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _loginService.ExecuteAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Auth.EmailNotVerified" => StatusCode(403, new { error = result.Error.Message }),
                _ => Unauthorized(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng nhập bằng Google OAuth — nhận Google ID Token từ NextAuth.js.
    /// POST /api/auth/google-login  { "idToken": "..." }
    /// </summary>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(GoogleLoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { error = "ID Token không được để trống." });

        var result = await _googleLoginService.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh JWT access token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _refreshTokenService.ExecuteAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng xuất — revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _logoutService.ExecuteAsync(request, ipAddress, cancellationToken);
        return Ok(new { message = "Logged out successfully" });
    }
}
