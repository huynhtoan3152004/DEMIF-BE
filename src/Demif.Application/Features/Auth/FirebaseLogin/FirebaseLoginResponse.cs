namespace Demif.Application.Features.Auth.FirebaseLogin;

/// <summary>
/// Response sau khi đăng nhập Firebase thành công
/// </summary>
public record FirebaseLoginResponse(
    /// <summary>
    /// User ID trong hệ thống DEMIF
    /// </summary>
    Guid UserId,
    
    /// <summary>
    /// Email của user
    /// </summary>
    string Email,
    
    /// <summary>
    /// Tên hiển thị của user
    /// </summary>
    string Username,
    
    /// <summary>
    /// JWT Access Token để gọi API
    /// </summary>
    string AccessToken,
    
    /// <summary>
    /// Danh sách roles của user
    /// </summary>
    List<string> Roles,
    
    /// <summary>
    /// User mới tạo hay đã có từ trước
    /// </summary>
    bool IsNewUser
);
