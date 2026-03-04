namespace Demif.Application.Features.Lessons.CheckSegment;

/// <summary>
/// Request: user gõ tự do những gì nghe được từ segment.
/// </summary>
public class CheckSegmentRequest
{
    /// <summary>
    /// Level đang làm bài — ảnh hưởng đến có trả transcript sau check không.
    /// "Beginner" | "Intermediate" | "Advanced" | "Expert"
    /// </summary>
    public string Level { get; set; } = "Intermediate";

    /// <summary>
    /// Toàn bộ text user gõ cho segment này (free-type, không chia blank).
    /// </summary>
    public string UserText { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian user làm (giây) — optional, dùng cho analytics.
    /// </summary>
    public int? TimeSpentSeconds { get; set; }
}

/// <summary>
/// Kết quả so sánh từng từ (word-by-word diff dùng LCS).
/// </summary>
public class WordCheckResult
{
    /// <summary>Từ gốc trong transcript (giữ nguyên case)</summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// "correct"  — user gõ đúng (sau khi normalize)
    /// "wrong"    — user gõ sai (có UserTyped)
    /// "skipped"  — user không gõ từ này
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Từ user thực sự gõ (chỉ có khi Status = "wrong")</summary>
    public string? UserTyped { get; set; }
}

/// <summary>
/// Response sau khi check segment — luôn trả transcript để user học từ lỗi.
/// </summary>
public class CheckSegmentResponse
{
    public int SegmentIndex { get; set; }
    public double Accuracy { get; set; }       // 0–100, làm tròn 1 chữ số
    public int CorrectCount { get; set; }
    public int TotalWords { get; set; }
    public int WrongCount { get; set; }
    public int SkippedCount { get; set; }

    /// <summary>
    /// Transcript gốc của segment — luôn trả về sau check (đây là học liệu chính).
    /// FE dùng để highlight diff bên cạnh input của user.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả từng từ — FE dùng để highlight màu (xanh/đỏ/xám).
    /// </summary>
    public List<WordCheckResult> WordResults { get; set; } = new();
}
