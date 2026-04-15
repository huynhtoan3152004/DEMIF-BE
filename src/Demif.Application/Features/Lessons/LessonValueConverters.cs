using System.Text.Json;
using System.Text.Json.Serialization;

namespace Demif.Application.Features.Lessons;

internal static class LessonValueNormalizer
{
    private static readonly string[] LevelNames = ["Beginner", "Intermediate", "Advanced", "Expert"];
    private static readonly string[] LessonTypeNames = ["Dictation", "Shadowing"];

    internal static bool TryNormalizeLevel(string? value, out string normalized)
        => TryNormalize(value, LevelNames, out normalized);

    internal static bool TryNormalizeLessonType(string? value, out string normalized)
        => TryNormalize(value, LessonTypeNames, out normalized);

    private static bool TryNormalize(string? value, string[] validValues, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out var numericValue))
        {
            if (numericValue >= 0 && numericValue < validValues.Length)
            {
                normalized = validValues[numericValue];
                return true;
            }

            return false;
        }

        var canonical = validValues.FirstOrDefault(item => item.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(canonical))
        {
            normalized = canonical;
            return true;
        }

        return false;
    }
}

internal sealed class LessonLevelJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => LessonValueNormalizer.TryNormalizeLevel(reader.GetInt32().ToString(), out var numericLevel)
                ? numericLevel
                : throw new JsonException("Level không hợp lệ."),
            JsonTokenType.String => LessonValueNormalizer.TryNormalizeLevel(reader.GetString(), out var level)
                ? level
                : throw new JsonException("Level không hợp lệ."),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException("Level phải là string hoặc number.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

internal sealed class LessonTypeJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => LessonValueNormalizer.TryNormalizeLessonType(reader.GetInt32().ToString(), out var numericType)
                ? numericType
                : throw new JsonException("LessonType không hợp lệ."),
            JsonTokenType.String => LessonValueNormalizer.TryNormalizeLessonType(reader.GetString(), out var lessonType)
                ? lessonType
                : throw new JsonException("LessonType không hợp lệ."),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException("LessonType phải là string hoặc number.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}