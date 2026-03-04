namespace Demif.Application.Features.Auth.Register;

/// <summary>
/// Response sau khi đăng ký — chỉ báo gửi email, KHÔNG trả JWT.
/// JWT chỉ được cấp sau khi xác nhận email thành công.
/// </summary>
public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = "Vui lòng kiểm tra email để xác nhận tài khoản.";
    public bool RequiresEmailVerification { get; set; } = true;
}
