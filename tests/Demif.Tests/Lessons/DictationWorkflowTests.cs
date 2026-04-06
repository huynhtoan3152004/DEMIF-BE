using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Lessons;
using Demif.Application.Features.Lessons.Admin;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Lessons.CheckSegment;
using Demif.Application.Features.Lessons.GetLessonSegments;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Demif.Tests.Lessons;

// ─── Helpers ────────────────────────────────────────────────────────────────

file static class LessonFactory
{
    public static Lesson Published(string transcript = "Hello everyone. Welcome to class today.")
    {
        var timed = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 30);
        return new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Test Lesson",
            Status = "published",
            DurationSeconds = 30,
            FullTranscript = transcript,
            TimedTranscript = timed,
            AudioUrl = "https://youtube.com/embed/test",
            MediaType = "youtube"
        };
    }

    public static Lesson Draft() => new Lesson
    {
        Id = Guid.NewGuid(),
        Title = "Draft Lesson",
        Status = "draft",
        DurationSeconds = 60
    };
}

// ─── VTT / SRT Parser Tests ─────────────────────────────────────────────────

public class VttParserTests
{
    [Fact]
    public void ParseVtt_StandardFormat_ReturnsCorrectSegments()
    {
        // Arrange
        var vtt = """
            WEBVTT

            00:00:02.500 --> 00:00:04.800
            Hello everyone.

            00:00:05.100 --> 00:00:08.300
            Welcome to today's lesson.
            """;

        // Act
        var segments = AdminTranscriptService.ParseVtt(vtt);

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal(2.5, segments[0].StartTime);
        Assert.Equal(4.8, segments[0].EndTime);
        Assert.Equal("Hello everyone.", segments[0].Text);
        Assert.Equal(5.1, segments[1].StartTime);
        Assert.Equal("Welcome to today's lesson.", segments[1].Text);
    }

    [Fact]
    public void ParseVtt_WithHtmlTags_StripsTagsFromText()
    {
        // Arrange — YouTube VTT thường có <c> tags
        var vtt = """
            WEBVTT

            00:00:01.000 --> 00:00:03.000
            <c>Hello</c> <c>world</c>

            """;

        // Act
        var segments = AdminTranscriptService.ParseVtt(vtt);

        // Assert
        Assert.Single(segments);
        Assert.Equal("Hello world", segments[0].Text);
    }

    [Fact]
    public void ParseVtt_MultilineSegment_JoinsLines()
    {
        // Arrange
        var vtt = """
            WEBVTT

            00:00:01.000 --> 00:00:05.000
            This is line one
            and line two.

            """;

        // Act
        var segments = AdminTranscriptService.ParseVtt(vtt);

        // Assert
        Assert.Single(segments);
        Assert.Equal("This is line one and line two.", segments[0].Text);
    }

    [Fact]
    public void ParseSrt_StandardFormat_ReturnsCorrectSegments()
    {
        // Arrange — SRT dùng dấu phẩy thay dấu chấm trong timestamp
        var srt = """
            1
            00:00:02,500 --> 00:00:04,800
            Hello everyone.

            2
            00:00:05,100 --> 00:00:08,300
            Welcome to class.
            """;

        // Act
        var segments = AdminTranscriptService.ParseSrt(srt);

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("Hello everyone.", segments[0].Text);
        Assert.Equal("Welcome to class.", segments[1].Text);
    }

    [Fact]
    public void ParseVtt_EmptyContent_ReturnsEmptyList()
    {
        var segments = AdminTranscriptService.ParseVtt("WEBVTT\n\n");
        Assert.Empty(segments);
    }

    [Fact]
    public void GenerateFromPlain_ProducesSegmentsWithTimestamps()
    {
        // Arrange
        var text = "Hello everyone. Welcome to class. Today we learn.";

        // Act
        var segments = AdminTranscriptService.GenerateFromPlain(text, 30);

        // Assert
        Assert.NotEmpty(segments);
        Assert.All(segments, s =>
        {
            Assert.True(s.EndTime > s.StartTime);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        });
    }
}

// ─── CheckSegmentService — LCS Algorithm Tests ──────────────────────────────

public class CheckSegmentServiceTests
{
    private static List<TimedSegment> MakeSegments(params string[] texts)
    {
        double t = 0;
        return texts.Select(text =>
        {
            var seg = new TimedSegment { StartTime = t, EndTime = t + 3, Text = text };
            t += 3;
            return seg;
        }).ToList();
    }

    private static CheckSegmentService BuildService(Lesson lesson, int segmentIndex = 0)
    {
        // Rebuild lesson with real segments based on its text
        var segments = MakeSegments(lesson.TimedTranscript is not null
            ? JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
                .Select(s => s.Text).ToArray()
            : new[] { "Hello everyone welcome to class." });

        var timedJson = JsonSerializer.Serialize(segments, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        lesson.TimedTranscript = timedJson;

        var lessonRepoMock = new Mock<ILessonRepository>();
        lessonRepoMock
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var dbMock = new Mock<IApplicationDbContext>();
        var dbSetMock = MockDbSet(new List<UserExercise>());
        dbMock.Setup(d => d.UserExercises).Returns(dbSetMock.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(1);

        return new CheckSegmentService(
            lessonRepoMock.Object,
            dbMock.Object,
            Mock.Of<ILogger<CheckSegmentService>>());
    }

    private static Mock<Microsoft.EntityFrameworkCore.DbSet<T>> MockDbSet<T>(List<T> data)
        where T : class
    {
        var mock = new Mock<Microsoft.EntityFrameworkCore.DbSet<T>>();
        mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        return mock;
    }

    [Fact]
    public async Task Check_AllCorrect_Returns100Percent()
    {
        // Arrange
        var lesson = LessonFactory.Published("Hello everyone welcome to class.");
        var service = BuildService(lesson);

        // Act
        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "Hello everyone welcome to class." },
            Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100.0, result.Value.Accuracy);
        Assert.All(result.Value.WordResults, w => Assert.Equal("correct", w.Status));
    }

    [Fact]
    public async Task Check_CaseInsensitive_MarksCorrect()
    {
        // Arrange — user gõ thường, transcript gốc có hoa
        var lesson = LessonFactory.Published("Hello Everyone Welcome.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "hello everyone welcome." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(100.0, result.Value.Accuracy);
    }

    [Fact]
    public async Task Check_SkippedWord_MarkedAsSkipped()
    {
        // Arrange: transcript "Hello everyone welcome to class"
        // User bỏ qua "to" → LCS xác định đúng
        var lesson = LessonFactory.Published("Hello everyone welcome to class.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "Hello everyone welcome class." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.WordResults, w => w.Status == "skipped");
        Assert.True(result.Value.SkippedCount >= 1);
    }

    [Fact]
    public async Task Check_WrongWord_MarkedWithUserTyped()
    {
        var lesson = LessonFactory.Published("Hello everyone welcome.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "Hello everone welcome." },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var wrong = result.Value.WordResults.First(w => w.Status == "wrong");
        Assert.Equal("everyone", wrong.Word.ToLowerInvariant());
        Assert.Equal("everone", wrong.UserTyped);
    }

    [Fact]
    public async Task Check_EmptyInput_AllSkipped()
    {
        var lesson = LessonFactory.Published("Hello everyone.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Advanced", UserText = "" },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(0.0, result.Value.Accuracy);
        Assert.All(result.Value.WordResults, w => Assert.Equal("skipped", w.Status));
    }

    [Fact]
    public async Task Check_AlwaysReturnsTranscript_ForLearning()
    {
        var lesson = LessonFactory.Published("Welcome to class.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Expert", UserText = "welcum to klass" },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Transcript));
    }

    [Fact]
    public async Task Check_InvalidSegmentIndex_ReturnsValidation()
    {
        var lesson = LessonFactory.Published("Hello.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(
            lesson.Id, 999,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task Check_DraftLesson_ReturnsNotFound()
    {
        var lesson = LessonFactory.Draft();
        lesson.TimedTranscript = JsonSerializer.Serialize(
            new[] { new TimedSegment { StartTime = 0, EndTime = 3, Text = "Hello." } },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var lessonRepoMock = new Mock<ILessonRepository>();
        lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lesson);
        var dbMock = new Mock<IApplicationDbContext>();

        var service = new CheckSegmentService(
            lessonRepoMock.Object, dbMock.Object,
            Mock.Of<ILogger<CheckSegmentService>>());

        var result = await service.ExecuteAsync(
            lesson.Id, 0,
            new CheckSegmentRequest { Level = "Intermediate", UserText = "Hello" },
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }
}

// ─── GetLessonSegmentsService Tests ─────────────────────────────────────────

public class GetLessonSegmentsServiceTests
{
    private GetLessonSegmentsService BuildService(Lesson lesson)
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(lesson);

        var subRepo = new Mock<IUserSubscriptionRepository>();
        subRepo.Setup(r => r.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<LessonSegmentsResponse?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                 .Returns<string, Func<CancellationToken, Task<LessonSegmentsResponse?>>, TimeSpan?, CancellationToken>(
                     async (k, f, t, ct) => await f(ct));

        return new GetLessonSegmentsService(lessonRepo.Object, subRepo.Object, cacheMock.Object, Mock.Of<IApplicationDbContext>());
    }

    [Fact]
    public async Task GetSegments_BeginnerLevel_TextIncluded()
    {
        // Arrange
        var lesson = LessonFactory.Published("Hello world. Welcome here.");
        var service = BuildService(lesson);

        // Act
        var result = await service.ExecuteAsync(lesson.Id, "Beginner");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.LevelConfig.ShowTranscriptBefore);
        Assert.All(result.Value.Segments, s => Assert.NotNull(s.Text));
    }

    [Fact]
    public async Task GetSegments_IntermediateLevel_TextHidden()
    {
        var lesson = LessonFactory.Published("Hello world. Welcome here.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(lesson.Id, "Intermediate");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.LevelConfig.ShowTranscriptBefore);
        Assert.All(result.Value.Segments, s => Assert.Null(s.Text));
    }

    [Fact]
    public async Task GetSegments_ExpertLevel_MaxReplays1()
    {
        var lesson = LessonFactory.Published("Test lesson.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(lesson.Id, "Expert");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.LevelConfig.MaxReplays);
        Assert.False(result.Value.LevelConfig.ShowTranscriptAfter);
    }

    [Fact]
    public async Task GetSegments_DraftLesson_ReturnsNotFound()
    {
        var lesson = LessonFactory.Draft();
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(lesson);

        var service = new GetLessonSegmentsService(
            lessonRepo.Object, Mock.Of<IUserSubscriptionRepository>(), Mock.Of<ICacheService>(), Mock.Of<IApplicationDbContext>());

        var result = await service.ExecuteAsync(lesson.Id, "Beginner");

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetSegments_InvalidLevel_ReturnsValidation()
    {
        var lesson = LessonFactory.Published("Hello.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(lesson.Id, "SuperHard");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("Beginner", true, true, -1)]
    [InlineData("Intermediate", false, true, 3)]
    [InlineData("Advanced", false, false, 2)]
    [InlineData("Expert", false, false, 1)]
    public async Task GetSegments_LevelConfig_MatchesExpected(
        string level, bool showBefore, bool showAfter, int maxReplays)
    {
        var lesson = LessonFactory.Published("Hello world. Test.");
        var service = BuildService(lesson);

        var result = await service.ExecuteAsync(lesson.Id, level);

        Assert.True(result.IsSuccess);
        var cfg = result.Value.LevelConfig;
        Assert.Equal(showBefore, cfg.ShowTranscriptBefore);
        Assert.Equal(showAfter, cfg.ShowTranscriptAfter);
        Assert.Equal(maxReplays, cfg.MaxReplays);
    }
}

// ─── AdminTranscriptService Tests ───────────────────────────────────────────

public class AdminTranscriptServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private AdminTranscriptService BuildService(Lesson? lesson = null)
    {
        var lessonRepo = new Mock<ILessonRepository>();
        if (lesson != null)
        {
            lessonRepo.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(lesson);
            lessonRepo.Setup(r => r.UpdateAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        }
        else
        {
            lessonRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Lesson?)null);
        }

        var dbMock = new Mock<IApplicationDbContext>();
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cacheMock = new Mock<ICacheService>();

        return new AdminTranscriptService(lessonRepo.Object, dbMock.Object, cacheMock.Object);
    }

    [Fact]
    public async Task UpdateTranscript_ValidVtt_ParsesAndSaves()
    {
        // Arrange
        var lesson = LessonFactory.Draft();
        var service = BuildService(lesson);
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:03.000\nHello world.\n\n";

        // Act
        var result = await service.UpdateTranscriptAsync(lesson.Id, new UpdateTranscriptRequest
        {
            Format = "vtt",
            RawContent = vtt
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.SegmentCount);
        Assert.Equal(2, result.Value.WordCount);
        Assert.Contains("✅", result.Value.Message);
    }

    [Fact]
    public async Task UpdateTranscript_ValidSrt_ParsesSuccessfully()
    {
        var lesson = LessonFactory.Draft();
        var service = BuildService(lesson);

        var result = await service.UpdateTranscriptAsync(lesson.Id, new UpdateTranscriptRequest
        {
            Format = "srt",
            RawContent = "1\n00:00:01,000 --> 00:00:03,000\nHello world.\n\n"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.SegmentCount);
    }

    [Fact]
    public async Task UpdateTranscript_PlainText_AutoGeneratesTimestamps()
    {
        var lesson = LessonFactory.Draft();
        var service = BuildService(lesson);

        var result = await service.UpdateTranscriptAsync(lesson.Id, new UpdateTranscriptRequest
        {
            Format = "plain",
            RawContent = "Hello everyone. Welcome to today's class."
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.SegmentCount >= 1);
    }

    [Fact]
    public async Task UpdateTranscript_EmptyContent_ReturnsValidation()
    {
        var lesson = LessonFactory.Draft();
        var service = BuildService(lesson);

        var result = await service.UpdateTranscriptAsync(lesson.Id, new UpdateTranscriptRequest
        {
            Format = "vtt", RawContent = ""
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatus_DraftToPublished_WithTranscript_Succeeds()
    {
        var lesson = LessonFactory.Published("Hello.");
        lesson.Status = "draft"; // đang draft, có transcript
        var service = BuildService(lesson);

        var result = await service.UpdateStatusAsync(lesson.Id,
            new UpdateLessonStatusRequest { Status = "published" });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateStatus_PublishWithoutTranscript_Blocked()
    {
        // Arrange — lesson không có TimedTranscript
        var lesson = LessonFactory.Draft(); // không có TimedTranscript
        var service = BuildService(lesson);

        // Act
        var result = await service.UpdateStatusAsync(lesson.Id,
            new UpdateLessonStatusRequest { Status = "published" });

        // Assert — phải bị block
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
        Assert.Contains("TimedTranscript", result.Error.Message);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsValidation()
    {
        var lesson = LessonFactory.Draft();
        var service = BuildService(lesson);

        var result = await service.UpdateStatusAsync(lesson.Id,
            new UpdateLessonStatusRequest { Status = "SomeRandomStatus" });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task GetDictationPreview_WithTranscript_ReturnsSegmentsAndAnswers()
    {
        var lesson = LessonFactory.Published("Hello everyone. Welcome here.");
        var service = BuildService(lesson);

        var result = await service.GetDictationPreviewAsync(lesson.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TotalSegments > 0);
        Assert.True(result.Value.ReadyToPublish);
        // Admin thấy FULL text (đáp án không bị ẩn)
        Assert.All(result.Value.Segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Text)));
    }

    [Fact]
    public async Task GetDictationPreview_WithoutTranscript_NotReadyToPublish()
    {
        var lesson = LessonFactory.Draft(); // không có TimedTranscript
        var service = BuildService(lesson);

        var result = await service.GetDictationPreviewAsync(lesson.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.ReadyToPublish);
        Assert.NotEmpty(result.Value.PublishBlockers);
    }
}
