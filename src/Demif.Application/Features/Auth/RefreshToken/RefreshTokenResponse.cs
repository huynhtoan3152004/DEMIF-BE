namespace Demif.Application.Features.Auth.RefreshToken;

/// <summary>
/// Response khi refresh token thành công
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
