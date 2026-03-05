namespace Demif.Application.Features.Me.GetStreak;

/// <summary>
/// Thông tin chuỗi ngày học liên tiếp
/// </summary>
public class GetStreakResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalActiveDays { get; set; }
    public int FreezesAvailable { get; set; }
    public DateTime? LastActiveDate { get; set; }

    /// <summary>true nếu user đã học hôm nay</summary>
    public bool LearnedToday { get; set; }
}
