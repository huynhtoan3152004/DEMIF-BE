using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Lessons;
using Demif.Application.Features.Lessons.CheckShadowing;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Demif.Tests.Lessons;

// ─── Helpers ─────────────────────────────────────────────────────────────────

file static class ShadowingLessonFactory
{
    public static Lesson Published(string transcript = "Hello everyone. Welcome to class today.")
    {
        var timed = BuildTimedTranscript(transcript);
        return new Lesson
        {
            Id              = Guid.NewGuid(),
            Title           = "Shadowing Test Lesson",
            Status          = "published",
            DurationSeconds = 30,
            FullTranscript  = transcript,
            TimedTranscript = timed,
            AudioUrl        = "https://youtube.com/embed/test",
            MediaType       = "youtube"
        };
    }

    public static Lesson Draft() => new()
    {
        Id              = Guid.NewGuid(),
        Title           = "Draft Lesson",
        Status          = "draft",
        DurationSeconds = 60
    };

    private static string BuildTimedTranscript(string text)
    {
        // Split into sentences
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().TrimEnd('.', '!', '?'))
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

        double t = 0;
        var segments = sentences.Select(s =>
        {
            var seg = new TimedSegment { Text = s + ".", StartTime = t, EndTime = t + 3.0 };
            t += 3.5;
            return seg;
        }).ToList();

        return JsonSerializer.Serialize(segments, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}

// ─── CheckShadowingService Tests ─────────────────────────────────────────────

public class CheckShadowingServiceTests
{
    private static CheckShadowingService BuildService(Lesson lesson)
    {
        var lessonRepoMock = new Mock<ILessonRepository>();
        lessonRepoMock
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var dbMock    = new Mock<IApplicationDbContext>();
        var dbSetMock = MockDbSet(new List<UserExercise>());
        dbMock.Setup(d => d.UserExercises).Returns(dbSetMock.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new CheckShadowingService(
            lessonRepoMock.Object,
            dbMock.Object,
            Mock.Of<ILogger<CheckShadowingService>>());
    }

    private static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        return mock;
    }

    // ── Happy paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Shadowing_PerfectMatch_Returns100Accuracy()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello everyone. Welcome to class.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = "Hello everyone." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(100.0, result.Value.Accuracy);
        Assert.True(result.Value.Passed);
        Assert.All(result.Value.WordResults, w => Assert.Equal("correct", w.Status));
    }

    [Fact]
    public async Task Shadowing_CaseInsensitive_MarksCorrect()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello Everyone Welcome.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Beginner", UserText = "hello everyone welcome." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(100.0, result.Value.Accuracy);
    }

    [Fact]
    public async Task Shadowing_SkippedWord_MarkedAsSkipped()
    {
        // Segment: "Hello everyone welcome to class."
        // User says: "Hello everyone welcome class." (skips "to")
        var lesson  = ShadowingLessonFactory.Published("Hello everyone welcome to class.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = "Hello everyone welcome class." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.WordResults, w => w.Status == "skipped");
        Assert.True(result.Value.SkippedCount >= 1);
        Assert.True(result.Value.Accuracy < 100);
    }

    [Fact]
    public async Task Shadowing_WrongWord_MarkedWithUserSpoken()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello everyone welcome.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Advanced", UserText = "Hello everone welcome." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var wrong = result.Value.WordResults.First(w => w.Status == "wrong");
        Assert.Equal("everyone", wrong.Word.ToLowerInvariant());
        Assert.Equal("everone", wrong.UserSpoken);
    }

    [Fact]
    public async Task Shadowing_EmptyUserText_AllSkipped_ZeroAccuracy()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello everyone.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Expert", UserText = "" },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(0.0, result.Value.Accuracy);
        Assert.False(result.Value.Passed);
        Assert.All(result.Value.WordResults, w => Assert.Equal("skipped", w.Status));
    }

    [Fact]
    public async Task Shadowing_AlwaysReturnsTargetText()
    {
        var lesson  = ShadowingLessonFactory.Published("Welcome to class today.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = "welcum to klass today" },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.TargetText));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.UserSpoke));
    }

    [Fact]
    public async Task Shadowing_PassedTrue_WhenAccuracyAbove70()
    {
        var lesson  = ShadowingLessonFactory.Published("Good morning, everyone. How are you today?");
        var service = BuildService(lesson);

        // User speaks 4/5 words correctly → ~80%
        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Beginner", UserText = "Good morning everyone. How are you today?" },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Accuracy >= 70);
        Assert.True(result.Value.Passed);
    }

    // ── Feedback messages ────────────────────────────────────────────────────

    [Theory]
    [InlineData("Good morning everyone how are you doing today I hope you are having a wonderful day.",
                "Good morning everyone how are you doing today I hope you are having a wonderful day.",
                "🌟")]
    public async Task Shadowing_ExcellentScore_GetsStarFeedback(string transcript, string userText, string expectedEmoji)
    {
        var lesson  = ShadowingLessonFactory.Published(transcript);
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = userText },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Contains(expectedEmoji, result.Value.Feedback);
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Shadowing_DraftLesson_ReturnsNotFound()
    {
        var lesson = ShadowingLessonFactory.Draft();
        var lessonRepoMock = new Mock<ILessonRepository>();
        lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lesson);

        var service = new CheckShadowingService(
            lessonRepoMock.Object,
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<ILogger<CheckShadowingService>>());

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Shadowing_InvalidSegmentIndex_ReturnsValidation()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 999,
            new CheckShadowingRequest { Level = "Beginner", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task Shadowing_InvalidLevel_ReturnsValidation()
    {
        var lesson  = ShadowingLessonFactory.Published("Hello.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "SuperHard", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task Shadowing_LessonNotFound_ReturnsNotFound()
    {
        var lessonRepoMock = new Mock<ILessonRepository>();
        lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Lesson?)null);

        var service = new CheckShadowingService(
            lessonRepoMock.Object,
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<ILogger<CheckShadowingService>>());

        var result = await service.ExecuteAsync(
            Guid.NewGuid(), 0,
            new CheckShadowingRequest { Level = "Beginner", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    // ── Word count metrics ───────────────────────────────────────────────────

    [Fact]
    public async Task Shadowing_WordCountsAreAccurate()
    {
        // "Hello everyone welcome to class." = 5 words
        var lesson  = ShadowingLessonFactory.Published("Hello everyone welcome to class.");
        var service = BuildService(lesson);

        // User says 3 words correctly, 1 wrong, 1 skipped
        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckShadowingRequest { Level = "Intermediate", UserText = "Hello everone welcome class." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(
            result.Value.TotalWords,
            result.Value.CorrectCount + result.Value.WrongCount + result.Value.SkippedCount);
    }
}
