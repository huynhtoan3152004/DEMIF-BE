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

    [Fact]
    public async Task GetOverviewAsync_ReturnsAggregatedStatsAndRecentItems()
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
                IsMastered = true,
                NextReviewAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserVocabulary
            {
                UserId = _userId,
                LessonId = lesson.Id,
                Topic = "travel",
                Word = "hotel",
                NormalizedWord = "hotel",
                NextReviewAt = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();

        var service = new VocabularyService(context);
        var result = await service.GetOverviewAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.DueCount);
        Assert.Equal(1, result.Value.MasteredCount);
        Assert.Equal(1, result.Value.LearningCount);
        Assert.Equal(2, result.Value.RecentItems.Count);
        Assert.Equal(1, result.Value.TopicCount);
        Assert.Equal(1, result.Value.LessonCount);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsTranscriptBasedCandidates()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "travel");
        lesson.FullTranscript = "The journey is long. The journey builds courage. Travel opens the mind.";
        context.Lessons.Add(lesson);

        context.UserVocabularies.Add(new UserVocabulary
        {
            UserId = _userId,
            LessonId = lesson.Id,
            Topic = "travel",
            Word = "journey",
            NormalizedWord = "journey"
        });

        await context.SaveChangesAsync();

        var service = new VocabularyService(context);
        var result = await service.GetSuggestionsAsync(_userId, lesson.Id, new VocabularySuggestionQuery { Limit = 5 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Contains(result.Value.Items, item => item.NormalizedWord == "journey" && item.IsAlreadySaved);
    }

    [Fact]
    public async Task ReviewAsync_WhenIncorrect_ResetsStreakAndInterval()
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
            NextReviewAt = DateTime.UtcNow.AddDays(-1),
            ConsecutiveCorrect = 3
        };

        context.UserVocabularies.Add(vocabulary);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        var result = await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = false });

        Assert.True(result.IsSuccess);
        
        var expectedNextReview = DateTime.UtcNow.AddDays(1);
        Assert.True(result.Value.NextReviewAt.HasValue);
        var diff = expectedNextReview - result.Value.NextReviewAt.Value;
        Assert.True(Math.Abs(diff.TotalMinutes) < 1);
    }

    [Fact]
    public async Task ReviewAsync_WhenEarlyReview_DoesNotAdvanceSchedule()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "academic");
        context.Lessons.Add(lesson);

        var originalNextReview = DateTime.UtcNow.AddDays(5);
        var vocabulary = new UserVocabulary
        {
            UserId = _userId,
            LessonId = lesson.Id,
            Topic = "academic",
            Word = "analysis",
            NormalizedWord = "analysis",
            NextReviewAt = originalNextReview,
            ConsecutiveCorrect = 2
        };

        context.UserVocabularies.Add(vocabulary);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        // Act: early review
        var result = await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.NextReviewAt.HasValue);
        var diff = originalNextReview - result.Value.NextReviewAt.Value;
        // The difference should be 0 since it wasn't updated.
        Assert.True(Math.Abs(diff.TotalMinutes) < 1);
    }
}