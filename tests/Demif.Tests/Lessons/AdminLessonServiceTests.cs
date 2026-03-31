using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Lessons.Admin;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Demif.Application.Abstractions.Services;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho AdminLessonService — CRUD + auto template generation
/// </summary>
public class AdminLessonServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<AdminLessonService>> _loggerMock;
    private readonly Mock<IValidator<UpdateLessonMetadataRequest>> _validatorMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly AdminLessonService _service;

    public AdminLessonServiceTests()
    {
        _lessonRepoMock = new Mock<ILessonRepository>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<AdminLessonService>>();
        _validatorMock = new Mock<IValidator<UpdateLessonMetadataRequest>>();
        _cacheMock = new Mock<ICacheService>();

        // Default: validation always passes
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateLessonMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _service = new AdminLessonService(
            _lessonRepoMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _validatorMock.Object,
            _cacheMock.Object);
    }

    private static UpdateLessonMetadataRequest CreateValidRequest()
    {
        return new UpdateLessonMetadataRequest
        {
            Title = "Business English: Meeting Vocabulary",
            Description = "Learn essential vocabulary for business meetings.",
            LessonType = LessonType.Dictation,
            Level = Level.Beginner,
            Category = "business",
            AudioUrl = "https://storage.example.com/lessons/meeting-vocab.mp3",
            IsPremiumOnly = false,
            Tags = "[\"business\", \"meeting\", \"vocabulary\"]"
        };
    }

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_ExistingLesson_ReturnsSuccess()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = new Lesson
        {
            Id = lessonId,
            Title = "Test Lesson",
            LessonType = LessonType.Dictation,
            Level = Level.Beginner,
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "Hello world.",
            DictationTemplates = "{\"Beginner\":{}}",
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Act
        var result = await _service.GetByIdAsync(lessonId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Lesson", result.Value.Title);
        Assert.True(result.Value.HasDictationTemplates);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingLesson_ReturnsNotFound()
    {
        // Arrange
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
        Assert.Contains("Không tìm thấy bài học", result.Error.Message);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_ExistingLesson_ReturnsSuccess()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = new Lesson
        {
            Id = lessonId,
            Title = "Old Title",
            FullTranscript = "Hello world.",
            DurationSeconds = 10,
            AudioUrl = "https://example.com/audio.mp3",
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var request = CreateValidRequest();
        request.Title = "New Title";
        var result = await _service.UpdateAsync(lessonId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", lesson.Title);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingLesson_ReturnsNotFound()
    {
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), CreateValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }



    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_ExistingLesson_SoftDeletes()
    {
        // Arrange
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "test",
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(lesson.Id);

        // Assert — Soft delete: status = "archived"
        Assert.True(result.IsSuccess);
        Assert.Equal("archived", lesson.Status);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingLesson_ReturnsNotFound()
    {
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    #endregion

    #region RegenerateTemplates Tests

    [Fact]
    public async Task RegenerateTemplatesAsync_ValidLesson_Regenerates()
    {
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            FullTranscript = "Hello everyone. Welcome to class.",
            DurationSeconds = 10,
            AudioUrl = "https://example.com/audio.mp3",
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.RegenerateTemplatesAsync(lesson.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(lesson.DictationTemplates);
    }

    [Fact]
    public async Task RegenerateTemplatesAsync_EmptyTranscript_ReturnsValidationError()
    {
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            FullTranscript = "",
            AudioUrl = "https://example.com/audio.mp3",
            Status = "published"
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.RegenerateTemplatesAsync(lesson.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
        Assert.Contains("FullTranscript", result.Error.Message);
    }

    [Fact]
    public async Task RegenerateTemplatesAsync_NonExistingLesson_ReturnsNotFound()
    {
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.RegenerateTemplatesAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    #endregion
}
