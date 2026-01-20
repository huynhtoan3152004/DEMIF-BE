namespace Demif.Application.Abstractions.Services;

/// <summary>
/// JWT Token Service interface
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role = "User");
    string GenerateRefreshToken();
    (bool isValid, Guid userId) ValidateAccessToken(string token);
}
