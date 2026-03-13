namespace Demif.Application.Features.Lessons.CheckShadowing;

/// <summary>
/// Request for shadowing check.
/// Supports 2 modes:
///   1. Text fallback: browser Web Speech API already transcribed → send UserText
///   2. Audio mode (future): send audio file → backend calls Whisper
/// </summary>
public class CheckShadowingRequest
{
    /// <summary>
    /// Text already transcribed by browser Web Speech API.
    /// Used in text-fallback mode (no Whisper needed).
    /// </summary>
    public string UserText { get; set; } = string.Empty;

    /// <summary>
    /// Level context: "Beginner" | "Intermediate" | "Advanced" | "Expert"
    /// </summary>
    public string Level { get; set; } = "Intermediate";

    /// <summary>
    /// Time user spent recording (seconds). Optional, for analytics.
    /// </summary>
    public int? TimeSpentSeconds { get; set; }
}

/// <summary>
/// Word-level comparison result for shadowing feedback.
/// </summary>
public class ShadowingWordResult
{
    /// <summary>Original word from transcript (preserves case)</summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// "correct"  — spoken correctly (after normalize)
    /// "wrong"    — spoken incorrectly (UserSpoken has what was said)
    /// "skipped"  — not spoken
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>What the user actually said (only when Status = "wrong")</summary>
    public string? UserSpoken { get; set; }
}

/// <summary>
/// Response after shadowing check.
/// Always returns target transcript so user can compare.
/// </summary>
public class CheckShadowingResponse
{
    public int SegmentIndex { get; set; }

    /// <summary>Overall pronunciation accuracy 0–100</summary>
    public double Accuracy { get; set; }

    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int SkippedCount { get; set; }
    public int TotalWords { get; set; }

    /// <summary>Plain-English feedback message based on score</summary>
    public string Feedback { get; set; } = string.Empty;

    /// <summary>Whether user passed this segment (accuracy >= 70)</summary>
    public bool Passed { get; set; }

    /// <summary>Target transcript — always returned so user can compare</summary>
    public string TargetText { get; set; } = string.Empty;

    /// <summary>What the speech-to-text heard from the user</summary>
    public string UserSpoke { get; set; } = string.Empty;

    /// <summary>Word-level diff for frontend to highlight in green/red/gray</summary>
    public List<ShadowingWordResult> WordResults { get; set; } = new();
}
