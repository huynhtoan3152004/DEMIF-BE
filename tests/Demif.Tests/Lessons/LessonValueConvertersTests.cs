using System.Text.Json;
using Demif.Application.Features.Lessons.Admin;

namespace Demif.Tests.Lessons;

public class LessonValueConvertersTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void QuickCreateRequest_NumericPayload_IsNormalizedToCanonicalNames()
    {
        var json = """
            {
                "title": "Test Lesson",
                "transcript": "Hello world.",
                "level": 3,
                "lessonType": 1
            }
            """;

        var request = JsonSerializer.Deserialize<QuickCreateLessonRequest>(json, WebJsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Expert", request!.Level);
        Assert.Equal("Shadowing", request.LessonType);
    }

    [Fact]
    public void UpdateMetadataRequest_NumericPayload_IsNormalizedToCanonicalNames()
    {
        var json = """
            {
                "title": "Test Lesson",
                "lessonType": 0,
                "level": 0,
                "audioUrl": "https://example.com/audio.mp3"
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateLessonMetadataRequest>(json, WebJsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Beginner", request!.Level);
        Assert.Equal("Dictation", request.LessonType);
    }
}