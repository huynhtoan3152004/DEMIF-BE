using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Demif.Application.Features.Blogs;

public static class BlogUtilities
{
    public static string CreateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "blog" : slug;
    }

    public static int EstimateReadingTimeMinutes(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        var words = Regex.Matches(content, "\\b\\w+\\b").Count;
        return Math.Max(1, (int)Math.Ceiling(words / 200d));
    }
}
