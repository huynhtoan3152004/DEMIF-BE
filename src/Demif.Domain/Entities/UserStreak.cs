using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserStreak - chuỗi ngày học liên tiếp
/// </summary>
public class UserStreak : BaseEntity
{
    public Guid UserId { get; set; }

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public int TotalActiveDays { get; set; }

    // Streak freeze
    public int FreezeCount { get; set; }
    public int FreezesAvailable { get; set; } = 1;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual User? User { get; set; }
}
