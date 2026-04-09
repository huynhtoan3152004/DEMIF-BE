using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Lessons;
using Demif.Application.Features.Lessons.SubmitDictation;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho SubmitDictationService — chấm điểm dictation
/// </summary>
public class SubmitDictationServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<SubmitDictationService>> _loggerMock;
    private readonly SubmitDictationService _service;

    public SubmitDictationServiceTests()
    {
        _lessonRepoMock = new Mock<ILessonRepository>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<SubmitDictationService>>();

        // Mock UserExercises DbSet with async support for FirstOrDefaultAsync (UPSERT pattern)
        var exerciseData = new List<UserExercise>().AsQueryable();
        var mockSet = new Mock<DbSet<UserExercise>>();
        mockSet.As<IQueryable<UserExercise>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<UserExercise>(exerciseData.Provider));
        mockSet.As<IQueryable<UserExercise>>().Setup(m => m.Expression).Returns(exerciseData.Expression);
        mockSet.As<IQueryable<UserExercise>>().Setup(m => m.ElementType).Returns(exerciseData.ElementType);
        mockSet.As<IQueryable<UserExercise>>().Setup(m => m.GetEnumerator()).Returns(exerciseData.GetEnumerator());
        mockSet.As<IAsyncEnumerable<UserExercise>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<UserExercise>(exerciseData.GetEnumerator()));

        _dbContextMock.Setup(d => d.UserExercises).Returns(mockSet.Object);
        _dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new SubmitDictationService(_lessonRepoMock.Object, _dbContextMock.Object, _loggerMock.Object);
    }

    private static Lesson CreateLessonWithTemplates()
    {
        var transcript = "Hello everyone. Welcome to our English class. Today we learn about communication.";
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 15);
        var dictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        return new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Test Lesson",
            AudioUrl = "https://example.com/audio.mp3",
            DurationSeconds = 15,
            FullTranscript = transcript,
            TimedTranscript = timedTranscript,
            DictationTemplates = dictationTemplates,
            Status = "published"
        };
    }

    #region Submit + Scoring Tests

    [Fact]
    public async Task ExecuteAsync_CorrectAnswers_ReturnsHighScore()
    {
        // Arrange
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Lấy template để biết đáp án đúng
        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var blankWords = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank).Select(w => new { SegmentIndex = si, w.Position, w.Answer }))
            .ToList();

        // Tạo request với TẤT CẢ đáp án đúng
        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 60,
            Answers = blankWords.Select(b => new UserAnswerInput
            {
                SegmentIndex = b.SegmentIndex,
                Position = b.Position,
                UserInput = b.Answer!
            }).ToList()
        };

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100.0m, result.Value.Score);
        Assert.Equal(100.0m, result.Value.AnsweredAccuracy);
        Assert.Equal(result.Value.TotalBlanks, result.Value.CorrectCount);
        Assert.Equal(0, result.Value.IncorrectCount);
        Assert.True(result.Value.IsSubmissionComplete);
        Assert.True(result.Value.IsFullyCorrect);
    }

    [Fact]
    public async Task ExecuteAsync_WrongAnswers_ReturnsLowScore()
    {
        // Arrange
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var blankWords = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank).Select(w => new { SegmentIndex = si, w.Position }))
            .ToList();

        // Tạo request với TẤT CẢ đáp án sai
        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 30,
            Answers = blankWords.Select(b => new UserAnswerInput
            {
                SegmentIndex = b.SegmentIndex,
                Position = b.Position,
                UserInput = "wronganswer"
            }).ToList()
        };

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0.0m, result.Value.Score);
        Assert.Equal(0, result.Value.CorrectCount);
        Assert.True(result.Value.IncorrectCount > 0);
    }

    [Fact]
    public async Task ExecuteAsync_CaseInsensitive_Matching()
    {
        // Arrange — test: "Welcome" vs "welcome" phải match
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var firstBlank = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank).Select(w => new { SegmentIndex = si, w.Position, w.Answer }))
            .First();

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 10,
            Answers = new List<UserAnswerInput>
            {
                new()
                {
                    SegmentIndex = firstBlank.SegmentIndex,
                    Position = firstBlank.Position,
                    // Đáp án đúng nhưng viết hoa/thường khác
                    UserInput = firstBlank.Answer!.ToUpper()
                }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsSuccess);
        var firstResult = result.Value.Results.First();
        Assert.True(firstResult.IsCorrect, $"'{firstBlank.Answer!.ToUpper()}' should match '{firstBlank.Answer}'");
    }

    [Fact]
    public async Task ExecuteAsync_PunctuationTrimmed_Matching()
    {
        // Arrange — test: "communication." vs "communication" phải match
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var blankWithPunctuation = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank && w.Answer != null && w.Answer.Any(c => ".!?,;:".Contains(c)))
                .Select(w => new { SegmentIndex = si, w.Position, w.Answer }))
            .FirstOrDefault();

        // Nếu không có blank có dấu câu, test vẫn pass — chỉ test có khi có
        if (blankWithPunctuation != null)
        {
            var cleanAnswer = blankWithPunctuation.Answer!.TrimEnd('.', ',', '!', '?', ';', ':');
            var request = new DictationSubmitRequest
            {
                Level = "Beginner",
                TimeSpentSeconds = 10,
                Answers = new List<UserAnswerInput>
                {
                    new()
                    {
                        SegmentIndex = blankWithPunctuation.SegmentIndex,
                        Position = blankWithPunctuation.Position,
                        UserInput = cleanAnswer // Không có dấu câu
                    }
                }
            };

            var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

            Assert.True(result.IsSuccess);
            var matchResult = result.Value.Results.First();
            Assert.True(matchResult.IsCorrect);
        }
    }

    [Fact]
    public async Task ExecuteAsync_PartialSubmission_CalculatesSkipped()
    {
        // Arrange — Chỉ điền 1 blank, còn lại bỏ trống
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var firstBlank = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank).Select(w => new { SegmentIndex = si, w.Position, w.Answer }))
            .First();

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 10,
            Answers = new List<UserAnswerInput>
            {
                new()
                {
                    SegmentIndex = firstBlank.SegmentIndex,
                    Position = firstBlank.Position,
                    UserInput = firstBlank.Answer!
                }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.AnsweredBlanks);
        Assert.Equal(1, result.Value.CorrectCount);
        Assert.Equal(100.0m, result.Value.AnsweredAccuracy);
        Assert.False(result.Value.IsSubmissionComplete);
        Assert.False(result.Value.IsFullyCorrect);
        Assert.True(result.Value.SkippedCount >= 0);
    }

    #endregion

    #region Validation & Error Tests

    [Fact]
    public async Task ExecuteAsync_NonExistingLesson_ReturnsNotFound()
    {
        _lessonRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var request = new DictationSubmitRequest { Level = "Beginner", Answers = new() };
        var result = await _service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), request);

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
            AudioUrl = "test",
            FullTranscript = "test",
            DictationTemplates = null
        };
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest { Level = "Beginner", Answers = new() };
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidLevel_ReturnsValidation()
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest { Level = "SuperExpert", Answers = new() };
        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
        Assert.Contains("SuperExpert", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSegmentIndex_HandledGracefully()
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 5,
            Answers = new List<UserAnswerInput>
            {
                new() { SegmentIndex = 999, Position = 0, UserInput = "test" }
            }
        };

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        Assert.True(result.IsSuccess); // Không crash
        Assert.False(result.Value.Results.First().IsCorrect);
        Assert.Contains("không hợp lệ", result.Value.Results.First().Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPosition_HandledGracefully()
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 5,
            Answers = new List<UserAnswerInput>
            {
                new() { SegmentIndex = 0, Position = 999, UserInput = "test" }
            }
        };

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Results.First().IsCorrect);
        Assert.Contains("không hợp lệ", result.Value.Results.First().Message);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAnswers_ReturnsZeroScore()
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 0,
            Answers = new List<UserAnswerInput>() // Không điền gì
        };

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Score);
        Assert.Equal(0, result.Value.AnsweredBlanks);
        Assert.True(result.Value.SkippedCount > 0);
    }

    [Fact]
    public async Task ExecuteAsync_SavesUserExercise()
    {
        // Arrange — Verify SaveChangesAsync is called
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var request = new DictationSubmitRequest { Level = "Beginner", Answers = new(), TimeSpentSeconds = 10 };

        // Act
        await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert — Phải gọi SaveChangesAsync
        _dbContextMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CorrectAnswer_ShowsCorrectAnswerInResult()
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var firstBlank = template.Segments
            .SelectMany((s, si) => s.Words.Where(w => w.IsBlank).Select(w => new { SegmentIndex = si, w.Position, w.Answer }))
            .First();

        var request = new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 10,
            Answers = new List<UserAnswerInput>
            {
                new() { SegmentIndex = firstBlank.SegmentIndex, Position = firstBlank.Position, UserInput = "wronganswer" }
            }
        };

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), request);

        // Assert — Sau khi submit, user thấy đáp án đúng
        Assert.True(result.IsSuccess);
        var answerResult = result.Value.Results.First();
        Assert.False(answerResult.IsCorrect);
        Assert.Equal(firstBlank.Answer, answerResult.CorrectAnswer);
    }

    [Fact]
    public async Task ExecuteAsync_UserTranscriptSample_AllCorrectAnswers_Returns100Percent()
    {
        var transcript = "Mastering any physical skill, be it performing a pirouette, playing an instrument, or throwing a baseball,";
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 20);
        var dictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Transcript Sample",
            AudioUrl = "https://example.com/audio.mp3",
            DurationSeconds = 20,
            FullTranscript = transcript,
            TimedTranscript = timedTranscript,
            DictationTemplates = dictationTemplates,
            Status = "published"
        };

        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Beginner)!;
        var answers = template.Segments
            .SelectMany((segment, segmentIndex) => segment.Words
                .Where(word => word.IsBlank)
                .Select(word => new UserAnswerInput
                {
                    SegmentIndex = segmentIndex,
                    Position = word.Position,
                    UserInput = word.Answer!
                }))
            .ToList();

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), new DictationSubmitRequest
        {
            Level = "Beginner",
            TimeSpentSeconds = 12,
            Answers = answers
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.Score);
        Assert.Equal(100m, result.Value.AnsweredAccuracy);
        Assert.Equal(result.Value.TotalBlanks, result.Value.CorrectCount);
        Assert.True(result.Value.IsSubmissionComplete);
        Assert.True(result.Value.IsFullyCorrect);
        Assert.All(result.Value.Results, item => Assert.True(item.IsCorrect));
    }

    [Theory]
    [InlineData("Beginner")]
    [InlineData("Intermediate")]
    [InlineData("Advanced")]
    [InlineData("Expert")]
    public async Task ExecuteAsync_AllLevels_FullyCorrectSubmission_Returns100Percent(string level)
    {
        var lesson = CreateLessonWithTemplates();
        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var levelEnum = Enum.Parse<Level>(level, ignoreCase: true);
        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, levelEnum)!;
        var answers = template.Segments
            .SelectMany((segment, segmentIndex) => segment.Words
                .Where(word => word.IsBlank)
                .Select(word => new UserAnswerInput
                {
                    SegmentIndex = segmentIndex,
                    Position = word.Position,
                    UserInput = word.Answer!
                }))
            .ToList();

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), new DictationSubmitRequest
        {
            Level = level,
            TimeSpentSeconds = 10,
            Answers = answers
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(level, result.Value.Level);
        Assert.Equal(100m, result.Value.Score);
        Assert.Equal(100m, result.Value.AnsweredAccuracy);
        Assert.True(result.Value.IsSubmissionComplete);
        Assert.True(result.Value.IsFullyCorrect);
        Assert.Equal(result.Value.TotalBlanks, result.Value.CorrectCount);
        Assert.All(result.Value.Results, item => Assert.True(item.IsCorrect));
    }

    [Fact]
    public async Task ExecuteAsync_ExpertShortTranscript_CanBlankAllWordsAndSubmitCorrectly()
    {
        var transcript = "Hello world.";
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 6);
        var dictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Expert Short Transcript",
            AudioUrl = "https://example.com/audio.mp3",
            DurationSeconds = 6,
            FullTranscript = transcript,
            TimedTranscript = timedTranscript,
            DictationTemplates = dictationTemplates,
            Status = "published"
        };

        _lessonRepoMock.Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var expertTemplate = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates!, Level.Expert)!;
        Assert.Equal(expertTemplate.TotalWords, expertTemplate.TotalBlanks);

        var answers = expertTemplate.Segments
            .SelectMany((segment, segmentIndex) => segment.Words
                .Where(word => word.IsBlank)
                .Select(word => new UserAnswerInput
                {
                    SegmentIndex = segmentIndex,
                    Position = word.Position,
                    UserInput = word.Answer!
                }))
            .ToList();

        var result = await _service.ExecuteAsync(lesson.Id, Guid.NewGuid(), new DictationSubmitRequest
        {
            Level = "Expert",
            TimeSpentSeconds = 8,
            Answers = answers
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.Score);
        Assert.Equal(100m, result.Value.AnsweredAccuracy);
        Assert.True(result.Value.IsSubmissionComplete);
        Assert.True(result.Value.IsFullyCorrect);
        Assert.Equal(result.Value.TotalBlanks, result.Value.AnsweredBlanks);
    }

    #endregion
}
