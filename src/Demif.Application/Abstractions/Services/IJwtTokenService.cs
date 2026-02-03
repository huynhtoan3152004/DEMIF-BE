namespace Demif.Application.Abstractions.Services;

/// <summary>
/// JWT Token Service interface
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Tạo Access Token với multiple roles
    /// </summary>
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);

    /// <summary>
    /// Tạo Refresh Token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate và parse Access Token
    /// </summary>
    (bool isValid, Guid userId, IEnumerable<string> roles) ValidateAccessToken(string token);
}

