namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Service verify Google ID Token từ FE (NextAuth.js)
/// Thay thế IFirebaseAuthService — không cần Firebase Admin SDK
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Verify Google ID Token và trả về thông tin user từ Google.
    /// Dùng Google public keys — không cần Firebase.
    /// </summary>
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken);
}

/// <summary>
/// Thông tin user lấy từ Google ID Token
/// </summary>
public record GoogleUserInfo(
    string GoogleId,      // sub claim
    string Email,
    string? Name,
    string? AvatarUrl,
    bool EmailVerified
);
