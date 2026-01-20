using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.FirebaseLogin;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Auth Controller - xử lý đăng nhập/đăng ký
/// Hỗ trợ cả Firebase Authentication và Email/Password
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginService _loginService;
    private readonly FirebaseLoginService _firebaseLoginService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<FirebaseLoginRequest> _firebaseLoginValidator;

    public AuthController(
        LoginService loginService,
        FirebaseLoginService firebaseLoginService,
        IValidator<LoginRequest> loginValidator,
        IValidator<FirebaseLoginRequest> firebaseLoginValidator)
    {
        _loginService = loginService;
        _firebaseLoginService = firebaseLoginService;
        _loginValidator = loginValidator;
        _firebaseLoginValidator = firebaseLoginValidator;
    }

    /// <summary>
    /// Đăng nhập bằng Firebase ID Token
    /// Flow: Client đăng nhập Firebase → Gửi ID Token → Server verify → Trả về JWT
    /// </summary>
    /// <param name="request">Firebase ID Token từ client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT Access Token và thông tin user</returns>
    [HttpPost("firebase-login")]
    [ProducesResponseType(typeof(FirebaseLoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> FirebaseLogin(
        [FromBody] FirebaseLoginRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Validate input
        var validationResult = await _firebaseLoginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        // 2. Call service
        var result = await _firebaseLoginService.ExecuteAsync(request, cancellationToken);

        // 3. Return result
        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng nhập bằng Email/Password (legacy - sẽ deprecated)
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Validate input
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        // 2. Call service
        var result = await _loginService.ExecuteAsync(request, cancellationToken);

        // 3. Return result
        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }
}
