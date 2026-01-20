using FirebaseAdmin.Auth;

namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Interface cho Firebase Authentication Service
/// Xử lý verify Firebase ID tokens từ client
/// </summary>
public interface IFirebaseAuthService
{
    /// <summary>
    /// Verify Firebase ID token và trả về thông tin user
    /// </summary>
    /// <param name="idToken">Firebase ID token từ client</param>
    /// <returns>FirebaseToken nếu hợp lệ</returns>
    Task<FirebaseToken> VerifyIdTokenAsync(string idToken);

    /// <summary>
    /// Lấy thông tin user từ Firebase bằng UID
    /// </summary>
    /// <param name="uid">Firebase User ID</param>
    /// <returns>UserRecord từ Firebase</returns>
    Task<UserRecord> GetUserByUidAsync(string uid);

    /// <summary>
    /// Lấy thông tin user từ Firebase bằng email
    /// </summary>
    /// <param name="email">Email của user</param>
    /// <returns>UserRecord từ Firebase</returns>
    Task<UserRecord> GetUserByEmailAsync(string email);
}
