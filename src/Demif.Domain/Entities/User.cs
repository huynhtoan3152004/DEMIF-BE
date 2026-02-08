using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity User - thông tin người dùng
/// </summary>
public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Profile info
    public string? Country { get; set; }
    public string NativeLanguage { get; set; } = "Vietnamese";
    public string TargetLanguage { get; set; } = "English";
    public Level CurrentLevel { get; set; } = Level.Beginner;
    public int DailyGoalMinutes { get; set; } = 30;

    // Firebase Auth Integration
    public string? FirebaseUid { get; set; }
    public string AuthProvider { get; set; } = "email";

    // Settings (JSON)
    public string? Settings { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual UserProgress? Progress { get; set; }
    public virtual UserStreak? Streak { get; set; }
    public virtual ICollection<UserExercise> Exercises { get; set; } = new List<UserExercise>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
