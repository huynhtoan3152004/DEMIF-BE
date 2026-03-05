using Demif.Domain.Enums;

namespace Demif.Application.Features.Me.RecordActivity;

/// <summary>
/// Ghi nhận 1 phiên học hoàn thành 
/// (Tự động cộng điểm, cập nhật streak)
/// </summary>
public class RecordActivityRequest
{
    /// <summary>Loại bài: Dictation | Shadowing</summary>
    public ExerciseType ExerciseType { get; set; }

    /// <summary>Điểm 0–100</summary>
    public int Score { get; set; }

    /// <summary>Số phút học trong phiên này</summary>
    public int MinutesSpent { get; set; }
}
