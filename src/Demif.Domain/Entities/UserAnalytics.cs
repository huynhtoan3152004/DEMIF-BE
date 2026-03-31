using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserAnalytics - Dashboard phân tích hành vi người dùng
/// Lưu trữ dữ liệu tổng hợp từ nhiều nguồn: UserExercise, UserProgress, UserStreak, Lesson, Payment
/// Dữ liệu được cập nhật định kỳ hoặc khi có activity mới
/// </summary>
public class UserAnalytics : BaseEntity
{
    public Guid UserId { get; set; }

    // ============================================
    // LEARNING STATISTICS (Thống kê học tập)
    // ============================================

    /// <summary>
    /// Tổng số bài tập đã hoàn thành
    /// </summary>
    public int TotalExercisesCompleted { get; set; }

    /// <summary>
    /// Tổng số bài học đã hoàn thành (unique lessons)
    /// </summary>
    public int TotalLessonsCompleted { get; set; }

    /// <summary>
    /// Tổng thời gian học (phút)
    /// </summary>
    public int TotalLearningMinutes { get; set; }

    /// <summary>
    /// Tổng số điểm tích lũy
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// Điểm trung bình Dictation (0-100)
    /// </summary>
    public decimal AvgDictationScore { get; set; }

    /// <summary>
    /// Điểm trung bình Shadowing (0-100)
    /// </summary>
    public decimal AvgShadowingScore { get; set; }

    /// <summary>
    /// Điểm cao nhất đạt được
    /// </summary>
    public int HighestScore { get; set; }

    /// <summary>
    /// Số lần đạt điểm hoàn hảo (100 điểm)
    /// </summary>
    public int PerfectScoresCount { get; set; }

    // ============================================
    // ACTIVITY PATTERNS (Mẫu hoạt động)
    // ============================================

    /// <summary>
    /// Ngày bắt đầu học (lần đầu tiên)
    /// </summary>
    public DateTime? FirstActivityDate { get; set; }

    /// <summary>
    /// Ngày hoạt động gần nhất
    /// </summary>
    public DateTime? LastActivityDate { get; set; }

    /// <summary>
    /// Tổng số ngày đã học (active days)
    /// </summary>
    public int TotalActiveDays { get; set; }

    /// <summary>
    /// Số ngày học liên tiếp hiện tại
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Số ngày học liên tiếp dài nhất
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// Số lần sử dụng streak freeze
    /// </summary>
    public int StreakFreezesUsed { get; set; }

    /// <summary>
    /// Số buổi học trung bình mỗi tuần
    /// </summary>
    public decimal AvgSessionsPerWeek { get; set; }

    /// <summary>
    /// Ngày trong tuần học nhiều nhất (0=Sunday, 6=Saturday)
    /// </summary>
    public int? MostActiveDayOfWeek { get; set; }

    /// <summary>
    /// Khung giờ học nhiều nhất (0-23)
    /// </summary>
    public int? MostActiveHour { get; set; }

    // ============================================
    // CONTENT ENGAGEMENT (Tương tác nội dung)
    // ============================================

    /// <summary>
    /// JSON object thống kê theo loại bài học (Dictation vs Shadowing)
    /// Format: {"Dictation": {"count": 50, "avgScore": 75}, "Shadowing": {"count": 30, "avgScore": 80}}
    /// </summary>
    public string? LessonTypeStats { get; set; }

    /// <summary>
    /// JSON object thống kê theo cấp độ (Beginner, Intermediate, Advanced, Expert)
    /// Format: {"Beginner": {"count": 20, "avgScore": 85}, "Intermediate": {"count": 30, "avgScore": 70}}
    /// </summary>
    public string? LevelStats { get; set; }

    /// <summary>
    /// JSON object thống kê theo danh mục (conversation, business, travel, academic)
    /// Format: {"conversation": {"count": 15, "avgScore": 78}, "business": {"count": 10, "avgScore": 72}}
    /// </summary>
    public string? CategoryStats { get; set; }

    /// <summary>
    /// JSON array các bài học yêu thích (top 5 bài học có điểm cao nhất)
    /// Format: [{"lessonId": "...", "title": "...", "score": 95, "completedAt": "..."}]
    /// </summary>
    public string? TopLessons { get; set; }

    /// <summary>
    /// JSON array các bài học gần đây (5 bài học gần nhất)
    /// Format: [{"lessonId": "...", "title": "...", "score": 85, "completedAt": "..."}]
    /// </summary>
    public string? RecentLessons { get; set; }

    // ============================================
    // PERFORMANCE TRENDS (Xu hướng hiệu suất)
    // ============================================

    /// <summary>
    /// JSON array xu hướng điểm theo tuần (12 tuần gần nhất)
    /// Format: [{"week": "2026-W10", "avgScore": 75, "exercisesCount": 5}]
    /// </summary>
    public string? WeeklyTrends { get; set; }

    /// <summary>
    /// JSON array xu hướng điểm theo tháng (6 tháng gần nhất)
    /// Format: [{"month": "2026-03", "avgScore": 78, "exercisesCount": 20}]
    /// </summary>
    public string? MonthlyTrends { get; set; }

    /// <summary>
    /// JSON object kỹ năng (từ UserProgress.Skills)
    /// Format: {"listening": 75, "speaking": 60, "vocabulary": 80, "grammar": 70}
    /// </summary>
    public string? SkillsBreakdown { get; set; }

    /// <summary>
    /// % cải thiện so với tuần trước
    /// </summary>
    public decimal WeeklyImprovement { get; set; }

    /// <summary>
    /// % cải thiện so với tháng trước
    /// </summary>
    public decimal MonthlyImprovement { get; set; }

    // ============================================
    // SUBSCRIPTION & PAYMENT ANALYTICS
    // ============================================

    /// <summary>
    /// Tier subscription hiện tại (Free, Premium, Pro)
    /// </summary>
    public string? CurrentSubscriptionTier { get; set; }

    /// <summary>
    /// Ngày hết hạn subscription hiện tại
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }

    /// <summary>
    /// Tổng số tiền đã thanh toán (VND)
    /// </summary>
    public decimal TotalAmountPaid { get; set; }

    /// <summary>
    /// Số lần thanh toán thành công
    /// </summary>
    public int SuccessfulPaymentsCount { get; set; }

    /// <summary>
    /// Ngày thanh toán gần nhất
    /// </summary>
    public DateTime? LastPaymentDate { get; set; }

    // ============================================
    // ENGAGEMENT METRICS (Chỉ số tương tác)
    // ============================================

    /// <summary>
    /// Số lần đăng nhập
    /// </summary>
    public int TotalLogins { get; set; }

    /// <summary>
    /// Ngày đăng nhập gần nhất
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Số bài viết blog đã xem
    /// </summary>
    public int BlogViewsCount { get; set; }

    /// <summary>
    /// Điểm engagement tổng hợp (0-100)
    /// Tính từ: streak, frequency, scores, consistency
    /// </summary>
    public int EngagementScore { get; set; }

    // ============================================
    // METADATA
    // ============================================

    /// <summary>
    /// Thời điểm cập nhật analytics gần nhất
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Phiên bản schema (để migration dữ liệu sau này)
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    // Navigation
    public virtual User? User { get; set; }
}
