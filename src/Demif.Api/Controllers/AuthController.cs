using Demif.Application.Features.Auth.Register;
using Demif.Application.Features.Auth.VerifyEmail;
using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.GoogleLogin;
using Demif.Application.Features.Auth.RefreshToken;
using Demif.Application.Features.Auth.Logout;
using Demif.Application.Features.Auth.ChangePassword;
using Demif.Application.Features.Auth.ForgotPassword;
using Demif.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Authentication — Register, Verify Email, Login (Email + Google), Token Management
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterService _registerService;
    private readonly VerifyEmailService _verifyEmailService;
    private readonly LoginService _loginService;
    private readonly GoogleLoginService _googleLoginService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly LogoutService _logoutService;
    private readonly ChangePasswordService _changePasswordService;
    private readonly ForgotPasswordService _forgotPasswordService;
    private readonly ResetPasswordService _resetPasswordService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        LoginService loginService,
        GoogleLoginService googleLoginService,
        VerifyEmailService verifyEmailService,
        RegisterService registerService,
        RefreshTokenService refreshTokenService,
        LogoutService logoutService,
        ChangePasswordService changePasswordService,
        ForgotPasswordService forgotPasswordService,
        ResetPasswordService resetPasswordService,
        ICurrentUserService currentUserService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _loginService = loginService;
        _googleLoginService = googleLoginService;
        _verifyEmailService = verifyEmailService;
        _registerService = registerService;
        _refreshTokenService = refreshTokenService;
        _logoutService = logoutService;
        _changePasswordService = changePasswordService;
        _forgotPasswordService = forgotPasswordService;
        _resetPasswordService = resetPasswordService;
        _currentUserService = currentUserService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    // ═══════════════════════════════════════════════════════════════
    // Registration & Email Verification
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Register a new account — sends verification email, does NOT return JWT.
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
    /// Verify email after registration — returns JWT for auto-login.
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

    // ═══════════════════════════════════════════════════════════════
    // Login (Email/Password + Google OAuth)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Login with Email/Password — requires verified email.
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
    /// Login with Google OAuth — receives Google ID Token from client.
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

    // ═══════════════════════════════════════════════════════════════
    // Token Management
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Refresh JWT access token using refresh token.
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
    /// Logout — revoke refresh token.
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

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _changePasswordService.ExecuteAsync(userId.Value, request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }

    /// <summary>
    /// Gửi email lấy lại mật khẩu
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _forgotPasswordService.ExecuteAsync(request, cancellationToken);
        // Luôn báo thành công kể cả có lỗi (Ví dụ không tìm thấy email) để bảo mật chống dò email
        return Ok(new { message = "Nếu email hợp lệ, link lấy lại mật khẩu đã được gửi." });
    }

    /// <summary>
    /// Reset mật khẩu qua Link
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _resetPasswordService.ExecuteAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }
        
        return Ok(new { message = "Khôi phục mật khẩu thành công. Vui lòng đăng nhập lại." });
    }
}
