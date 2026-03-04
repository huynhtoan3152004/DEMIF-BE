using Demif.Application.Abstractions.Services;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// Google Auth Service — verify Google ID Token từ NextAuth.js.
/// Dùng Google.Apis.Auth library — không cần Firebase Admin SDK.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string _clientId;

    public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
        _clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured.");
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            _logger.LogInformation("Google token verified for: {Email}", payload.Email);

            return new GoogleUserInfo(
                GoogleId: payload.Subject,
                Email: payload.Email,
                Name: payload.Name,
                AvatarUrl: payload.Picture,
                EmailVerified: payload.EmailVerified
            );
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google ID token: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Google ID Token không hợp lệ hoặc đã hết hạn.");
        }
    }
}
