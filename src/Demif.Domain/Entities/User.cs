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

    // OAuth Integration
    public string? GoogleId { get; set; }
    public string AuthProvider { get; set; } = "email"; // email | google

    // Email Verification
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationExpiry { get; set; }

    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

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
    public virtual ICollection<UserVocabulary> Vocabularies { get; set; } = new List<UserVocabulary>();
}
