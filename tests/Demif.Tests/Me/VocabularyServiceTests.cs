using Demif.Application.Features.Me.Vocabulary;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Demif.Tests.Helpers;

namespace Demif.Tests.Me;

public class VocabularyServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static Lesson CreatePublishedLesson(Guid lessonId, string? category = "travel")
    {
        return new Lesson
        {
            Id = lessonId,
            Title = "Travel Lesson",
            Category = category,
            AudioUrl = "https://example.com/audio.mp3",
            DurationSeconds = 60,
            FullTranscript = "Sample transcript",
            Status = "published"
        };
    }

    [Fact]
    public async Task SaveAsync_WhenTopicMissing_UsesLessonCategory()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "travel");
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        var result = await service.SaveAsync(_userId, new SaveVocabularyRequest
        {
            LessonId = lesson.Id,
            Word = "Journey",
            Meaning = "hành trình"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("travel", result.Value.Topic);
        Assert.Equal("Journey", result.Value.Word);
        Assert.Single(context.UserVocabularies);
        Assert.NotNull(result.Value.NextReviewAt);
    }

    [Fact]
    public async Task SaveAsync_WhenSameWordSavedTwice_UpsertsExistingItem()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "business");
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        var first = await service.SaveAsync(_userId, new SaveVocabularyRequest
        {
            LessonId = lesson.Id,
            Word = "Revenue",
            Topic = "business",
            Meaning = "doanh thu"
        });

        var second = await service.SaveAsync(_userId, new SaveVocabularyRequest
        {
            LessonId = lesson.Id,
            Word = " revenue ",
            Topic = "business",
            Meaning = "thu nhập"
        });

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(context.UserVocabularies);
        Assert.Equal("thu nhập", context.UserVocabularies.First().Meaning);
    }

    [Fact]
    public async Task GetAsync_DueOnly_ReturnsOnlyDueItems()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "travel");
        context.Lessons.Add(lesson);

        context.UserVocabularies.AddRange(
            new UserVocabulary
            {
                UserId = _userId,
                LessonId = lesson.Id,
                Topic = "travel",
                Word = "airport",
                NormalizedWord = "airport",
                NextReviewAt = DateTime.UtcNow.AddHours(-1)
            },
            new UserVocabulary
            {
                UserId = _userId,
                LessonId = lesson.Id,
                Topic = "travel",
                Word = "hotel",
                NormalizedWord = "hotel",
                NextReviewAt = DateTime.UtcNow.AddDays(2)
            });

        await context.SaveChangesAsync();

        var service = new VocabularyService(context);
        var result = await service.GetAsync(_userId, new VocabularyQueryRequest(), dueOnly: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("airport", result.Value.Items[0].Word);
    }

    [Fact]
    public async Task ReviewAsync_WhenCorrect_AdvancesScheduleAndCanMasterWord()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "academic");
        context.Lessons.Add(lesson);

        var vocabulary = new UserVocabulary
        {
            UserId = _userId,
            LessonId = lesson.Id,
            Topic = "academic",
            Word = "analysis",
            NormalizedWord = "analysis",
            NextReviewAt = DateTime.UtcNow.AddDays(-1)
        };

        context.UserVocabularies.Add(vocabulary);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });
        await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });
        var result = await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.ReviewCount);
        Assert.True(result.Value.CorrectReviews >= 3);
        Assert.True(result.Value.IsMastered);
        Assert.NotNull(result.Value.MasteredAt);
        Assert.True(result.Value.NextReviewAt > DateTime.UtcNow);
    }
}