using System.Text.Json;
using System.Text.Json.Serialization;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons;

/// <summary>
/// Auto-generate DictationTemplate từ FullTranscript + DurationSeconds.
/// 1. Tạo TimedTranscript: split text thành sentences → phân bổ thời gian theo word count
/// 2. Tạo DictationTemplate cho mỗi Level: chọn từ để ẩn theo blankPercentage
/// </summary>
public static class DictationTemplateGenerator
{
    // Tỷ lệ blanks cho mỗi Level
    private static readonly Dictionary<Level, DifficultyConfig> DifficultyConfigs = new()
    {
        [Level.Beginner] = new(15, HintType.FirstLetterAndLength),
        [Level.Intermediate] = new(35, HintType.FirstLetter),
        [Level.Advanced] = new(55, HintType.LengthOnly),
        [Level.Expert] = new(80, HintType.None)
    };

    // Danh sách function words (ít quan trọng, ẩn sau)
    private static readonly HashSet<string> FunctionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "am", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "shall", "should",
        "may", "might", "must", "can", "could", "to", "of", "in", "for", "on", "with",
        "at", "by", "from", "as", "into", "about", "but", "or", "and", "not", "no",
        "if", "then", "so", "than", "too", "very", "just", "that", "this", "these",
        "those", "it", "its", "i", "you", "he", "she", "we", "they", "me", "him",
        "her", "us", "them", "my", "your", "his", "our", "their", "what", "which",
        "who", "when", "where", "how", "all", "each", "every", "both", "few", "more",
        "some", "any", "most", "other", "up", "out", "also"
    };

    /// <summary>
    /// Tạo TimedTranscript từ FullTranscript + DurationSeconds.
    /// Split theo câu (dấu chấm, chấm than, chấm hỏi) → phân bổ thời gian theo word count.
    /// </summary>
    public static string GenerateTimedTranscript(string fullTranscript, int durationSeconds)
    {
        var sentences = SplitIntoSentences(fullTranscript);
        if (sentences.Count == 0)
            return "[]";

        var totalWords = sentences.Sum(s => CountWords(s));
        if (totalWords == 0) totalWords = 1;

        var segments = new List<TimedSegment>();
        double currentTime = 0;

        foreach (var sentence in sentences)
        {
            var wordCount = CountWords(sentence);
            // Thời gian tỉ lệ theo số từ
            var segmentDuration = (double)wordCount / totalWords * durationSeconds;
            // Tối thiểu 1 giây
            segmentDuration = Math.Max(segmentDuration, 1.0);

            segments.Add(new TimedSegment
            {
                StartTime = Math.Round(currentTime, 1),
                EndTime = Math.Round(currentTime + segmentDuration, 1),
                Text = sentence.Trim()
            });

            currentTime += segmentDuration;
        }

        // Điều chỉnh segment cuối cùng để khớp với tổng duration
        if (segments.Count > 0)
        {
            segments[^1].EndTime = durationSeconds;
        }

        return JsonSerializer.Serialize(segments, JsonOptions);
    }

    /// <summary>
    /// Tạo DictationTemplates cho tất cả Levels từ TimedTranscript.
    /// </summary>
    public static string GenerateAllTemplates(string timedTranscriptJson)
    {
        var segments = JsonSerializer.Deserialize<List<TimedSegment>>(timedTranscriptJson, JsonOptions);
        if (segments == null || segments.Count == 0)
            return "{}";

        var templates = new Dictionary<string, DictationTemplate>();

        foreach (var level in Enum.GetValues<Level>())
        {
            var config = DifficultyConfigs[level];
            templates[level.ToString()] = GenerateTemplate(segments, level, config);
        }

        return JsonSerializer.Serialize(templates, JsonOptions);
    }

    /// <summary>
    /// Lấy template cho 1 level cụ thể.
    /// </summary>
    public static DictationTemplate? GetTemplateForLevel(string dictationTemplatesJson, Level level)
    {
        var templates = JsonSerializer.Deserialize<Dictionary<string, DictationTemplate>>(dictationTemplatesJson, JsonOptions);
        return templates?.GetValueOrDefault(level.ToString());
    }

    private static DictationTemplate GenerateTemplate(List<TimedSegment> segments, Level level, DifficultyConfig config)
    {
        var templateSegments = new List<DictationSegment>();
        var totalBlanks = 0;
        var totalWords = 0;

        foreach (var segment in segments)
        {
            var words = segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var dictationWords = new List<DictationWord>();

            // Xác định từ nào cần ẩn
            var wordsToBlank = SelectWordsToBlank(words, config.BlankPercentage, level);

            for (var i = 0; i < words.Length; i++)
            {
                var isBlank = wordsToBlank.Contains(i);
                var cleanWord = words[i].TrimEnd('.', ',', '!', '?', ';', ':');
                var punctuation = words[i].Length > cleanWord.Length
                    ? words[i][cleanWord.Length..]
                    : "";

                var dw = new DictationWord
                {
                    Text = isBlank ? "" : words[i],
                    IsBlank = isBlank,
                    Position = i,
                    Punctuation = punctuation
                };

                if (isBlank)
                {
                    dw.Answer = words[i];
                    dw.Hint = GenerateHint(cleanWord, config.HintType);
                    dw.Length = cleanWord.Length;
                    totalBlanks++;
                }

                dictationWords.Add(dw);
                totalWords++;
            }

            templateSegments.Add(new DictationSegment
            {
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                OriginalText = segment.Text,
                Words = dictationWords
            });
        }

        return new DictationTemplate
        {
            Level = level.ToString(),
            BlankPercentage = config.BlankPercentage,
            Segments = templateSegments,
            TotalBlanks = totalBlanks,
            TotalWords = totalWords
        };
    }

    /// <summary>
    /// Chọn index các từ cần ẩn. Ưu tiên content words trước, function words sau.
    /// </summary>
    private static HashSet<int> SelectWordsToBlank(string[] words, int blankPercentage, Level level)
    {
        var result = new HashSet<int>();
        var targetBlanks = Math.Max(1, (int)Math.Round(words.Length * blankPercentage / 100.0));

        if (words.Length <= 1)
            return result;

        // Tách index thành content words và function words
        var contentWordIndices = new List<int>();
        var functionWordIndices = new List<int>();

        for (var i = 0; i < words.Length; i++)
        {
            var cleanWord = words[i].TrimEnd('.', ',', '!', '?', ';', ':');
            if (cleanWord.Length < 2) continue; // Bỏ qua từ 1 ký tự

            if (FunctionWords.Contains(cleanWord))
                functionWordIndices.Add(i);
            else
                contentWordIndices.Add(i);
        }

        // Seed random nhất quán theo text (để cùng text luôn cho cùng kết quả)
        var seed = words.Aggregate(0, (acc, w) => acc ^ w.GetHashCode()) + (int)level;
        var rng = new Random(seed);

        // Shuffle content words
        Shuffle(contentWordIndices, rng);
        Shuffle(functionWordIndices, rng);

        // Ưu tiên content words trước
        foreach (var idx in contentWordIndices)
        {
            if (result.Count >= targetBlanks) break;
            result.Add(idx);
        }

        // Nếu chưa đủ, thêm function words (cho Expert level)
        foreach (var idx in functionWordIndices)
        {
            if (result.Count >= targetBlanks) break;
            result.Add(idx);
        }

        return result;
    }

    private static string GenerateHint(string word, HintType hintType)
    {
        return hintType switch
        {
            HintType.FirstLetterAndLength => $"{word[0]}{"".PadRight(word.Length - 1, '_')}",
            HintType.FirstLetter => $"{word[0]}___",
            HintType.LengthOnly => $"{"".PadRight(word.Length, '_')}",
            HintType.None => "____",
            _ => "____"
        };
    }

    private static List<string> SplitIntoSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Split theo dấu câu, giữ lại dấu câu trong sentence
        var sentences = new List<string>();
        var current = "";

        foreach (var ch in text)
        {
            current += ch;
            if (ch is '.' or '!' or '?')
            {
                var trimmed = current.Trim();
                if (trimmed.Length > 0)
                    sentences.Add(trimmed);
                current = "";
            }
        }

        // Phần còn lại (nếu text không kết thúc bằng dấu câu)
        var remaining = current.Trim();
        if (remaining.Length > 0)
            sentences.Add(remaining);

        // Nếu không có dấu câu nào, chia theo khoảng ~10 từ
        if (sentences.Count <= 1 && CountWords(text) > 15)
        {
            sentences.Clear();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chunkSize = 10;
            for (var i = 0; i < words.Length; i += chunkSize)
            {
                var chunk = string.Join(' ', words.Skip(i).Take(chunkSize));
                sentences.Add(chunk);
            }
        }

        return sentences;
    }

    private static int CountWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // JSON options
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

#region DTOs

public class TimedSegment
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class DictationTemplate
{
    [JsonConverter(typeof(FlexibleLevelStringConverter))]
    public string Level { get; set; } = string.Empty;
    public int BlankPercentage { get; set; }
    public List<DictationSegment> Segments { get; set; } = new();
    public int TotalBlanks { get; set; }
    public int TotalWords { get; set; }
}

public class DictationSegment
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public List<DictationWord> Words { get; set; } = new();
}

public class DictationWord
{
    public string Text { get; set; } = string.Empty;
    public bool IsBlank { get; set; }
    public int Position { get; set; }
    public string? Answer { get; set; }
    public string? Hint { get; set; }
    public int? Length { get; set; }
    public string? Punctuation { get; set; }
}

public record DifficultyConfig(int BlankPercentage, HintType HintType);

public enum HintType
{
    FirstLetterAndLength,
    FirstLetter,
    LengthOnly,
    None
}

public sealed class FlexibleLevelStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var numericLevel) => NormalizeLevelName(numericLevel),
            JsonTokenType.String => NormalizeLevelName(reader.GetString()),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException("Level phải là số hoặc chuỗi hợp lệ.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var normalized = NormalizeLevelName(value);
        writer.WriteStringValue(string.IsNullOrWhiteSpace(normalized) ? value : normalized);
    }

    private static string NormalizeLevelName(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        if (Enum.TryParse<Level>(rawValue, true, out var parsedLevel))
            return parsedLevel.ToString();

        return rawValue.Trim();
    }

    private static string NormalizeLevelName(int numericLevel)
    {
        return Enum.IsDefined(typeof(Level), numericLevel)
            ? ((Level)numericLevel).ToString()
            : throw new JsonException($"Level numeric value '{numericLevel}' is not valid.");
    }
}

#endregion
