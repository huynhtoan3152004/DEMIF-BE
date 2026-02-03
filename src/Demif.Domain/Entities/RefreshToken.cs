using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity RefreshToken - quản lý refresh token cho JWT authentication
/// Hỗ trợ token rotation và revocation
/// </summary>
public class RefreshToken : AuditableEntity
{
    /// <summary>
    /// Giá trị refresh token (Base64 encoded)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User ID sở hữu token
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Thời điểm token hết hạn
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Thời điểm token bị revoke (null = chưa bị revoke)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Token thay thế khi rotate (null = chưa được thay thế)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Lý do revoke token
    /// </summary>
    public string? RevokeReason { get; set; }

    /// <summary>
    /// IP address của client tạo token
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address của client revoke token
    /// </summary>
    public string? RevokedByIp { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;

    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
