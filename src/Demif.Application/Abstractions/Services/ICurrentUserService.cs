namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Current User Service - lấy thông tin user đang đăng nhập từ token
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
