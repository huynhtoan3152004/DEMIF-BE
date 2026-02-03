using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.FirebaseLogin;
using Demif.Application.Features.Auth.Register;
using Demif.Application.Features.Auth.RefreshToken;
using Demif.Application.Features.Auth.Logout;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Auth Controller - xử lý đăng nhập/đăng ký/refresh token
/// Hỗ trợ cả Firebase Authentication và Email/Password
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginService _loginService;
    private readonly FirebaseLoginService _firebaseLoginService;
    private readonly RegisterService _registerService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly LogoutService _logoutService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<FirebaseLoginRequest> _firebaseLoginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthController(
        LoginService loginService,
        FirebaseLoginService firebaseLoginService,
        RegisterService registerService,
        RefreshTokenService refreshTokenService,
        LogoutService logoutService,
        IValidator<LoginRequest> loginValidator,
        IValidator<FirebaseLoginRequest> firebaseLoginValidator,
        IValidator<RegisterRequest> registerValidator)
    {
        _loginService = loginService;
        _firebaseLoginService = firebaseLoginService;
        _registerService = registerService;
        _refreshTokenService = refreshTokenService;
        _logoutService = logoutService;
        _loginValidator = loginValidator;
        _firebaseLoginValidator = firebaseLoginValidator;
        _registerValidator = registerValidator;
    }

    /// <summary>
    /// Đăng ký tài khoản mới
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
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

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
    /// Đăng nhập bằng Email/Password
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
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _loginService.ExecuteAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng nhập bằng Firebase ID Token
    /// </summary>
    [HttpPost("firebase-login")]
    [ProducesResponseType(typeof(FirebaseLoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> FirebaseLogin(
        [FromBody] FirebaseLoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _firebaseLoginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var result = await _firebaseLoginService.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token
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
        {
            return Unauthorized(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng xuất - revoke refresh token
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

