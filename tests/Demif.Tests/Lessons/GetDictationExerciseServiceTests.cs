using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Lessons;
using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho GetDictationExerciseService — lấy bài tập dictation
/// </summary>
public class GetDictationExerciseServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetDictationExerciseService _service;

    public GetDictationExerciseServiceTests()
    {
        _lessonRepoMock = new Mock<ILessonRepository>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _service = new GetDictationExerciseService(_lessonRepoMock.Object, _dbContextMock.Object);
    }

    private static Lesson CreateLessonWithTemplates(bool isPremium = false)
    {
        var transcript = "Hello everyone. Welcome to our English class. Today we learn business vocabulary.";
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 15);
        var dictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        return new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Business English 101",
            Description = "Learn business vocabulary.",
            LessonType = LessonType.Dictation,
            Level = Level.Beginner,
            AudioUrl = "https://example.com/audio.mp3",
            DurationSeconds = 15,
            FullTranscript = transcript,
            TimedTranscript = timedTranscript,
            DictationTemplates = dictationTemplates,
            IsPremiumOnly = isPremium,
            Status = "published"
        };
    }

    [Fact]
    public async Task ExecuteAsync_ValidLesson_ReturnsTemplate()
    {
        // Arrange
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Level.Beginner);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(lesson.Id, result.Value.LessonId);
        Assert.Equal("Beginner", result.Value.Level);
        Assert.NotNull(result.Value.Template);
        Assert.True(result.Value.Template.TotalBlanks > 0);
    }

    [Fact]
    public async Task ExecuteAsync_AnswersAreStripped()
    {
        // Arrange — Đáp án PHẢI bị xóa khỏi response
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Level.Beginner);

        // Assert — Tất cả blank words phải có Answer = null
        Assert.True(result.IsSuccess);
        var blankWords = result.Value.Template.Segments
            .SelectMany(s => s.Words)
            .Where(w => w.IsBlank)
            .ToList();

        Assert.All(blankWords, w => Assert.Null(w.Answer));
    }

    [Fact]
    public async Task ExecuteAsync_NonExistingLesson_ReturnsNotFound()
    {
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.ExecuteAsync(Guid.NewGuid(), Level.Beginner);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
        Assert.Contains("Không tìm thấy bài học", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoDictationTemplates_ReturnsNotFound()
    {
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "No Templates",
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "test",
            DictationTemplates = null, // Chưa generate templates
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.ExecuteAsync(lesson.Id, Level.Beginner);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
        Assert.Contains("Dictation template", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_PremiumLesson_NoUser_ReturnsForbidden()
    {
        var lesson = CreateLessonWithTemplates(isPremium: true);
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Act — Không có userId
        var result = await _service.ExecuteAsync(lesson.Id, Level.Beginner, userId: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error.Code);
        Assert.Contains("Premium", result.Error.Message);
    }

    [Theory]
    [InlineData(Level.Beginner)]
    [InlineData(Level.Intermediate)]
    [InlineData(Level.Advanced)]
    [InlineData(Level.Expert)]
    public async Task ExecuteAsync_AllLevels_ReturnValidTemplate(Level level)
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.ExecuteAsync(lesson.Id, level);

        Assert.True(result.IsSuccess);
        Assert.Equal(level.ToString(), result.Value.Level);
        Assert.NotNull(result.Value.Template);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAudioUrlPreferringMediaUrl()
    {
        var lesson = CreateLessonWithTemplates();
        lesson.MediaUrl = "https://cdn.example.com/video.mp4";
        lesson.AudioUrl = "https://storage.example.com/audio.mp3";
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.ExecuteAsync(lesson.Id, Level.Beginner);

        Assert.True(result.IsSuccess);
        // MediaUrl ưu tiên hơn AudioUrl
        Assert.Equal("https://cdn.example.com/video.mp4", result.Value.AudioUrl);
    }
}
