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
        Assert.Equal("new", result.Value.ReviewStatus);
        Assert.True(result.Value.NextReviewAt.HasValue);
        Assert.True(result.Value.NextReviewAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SaveAsync_NewItemIsScheduledForTomorrow_AndNotDueImmediately()
    {
        var context = CreateDbContext();
        var lesson = CreatePublishedLesson(Guid.NewGuid(), "travel");
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        var service = new VocabularyService(context);

        var saveResult = await service.SaveAsync(_userId, new SaveVocabularyRequest
        {
            LessonId = lesson.Id,
            Word = "Journey",
            Topic = "travel",
            Meaning = "hành trình"
        });

        Assert.True(saveResult.IsSuccess);

        var dueResult = await service.GetAsync(_userId, new VocabularyQueryRequest(), dueOnly: true);

        Assert.True(dueResult.IsSuccess);
        Assert.Empty(dueResult.Value.Items);
        Assert.Equal("new", saveResult.Value.ReviewStatus);
        Assert.True(saveResult.Value.NextReviewAt > DateTime.UtcNow);
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
        vocabulary.NextReviewAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });
        vocabulary.NextReviewAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        var result = await service.ReviewAsync(_userId, vocabulary.Id, new ReviewVocabularyRequest { IsCorrect = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.ReviewCount);
        Assert.True(result.Value.CorrectReviews >= 3);
        Assert.True(result.Value.IsMastered);
        Assert.NotNull(result.Value.MasteredAt);
        Assert.True(result.Value.NextReviewAt > DateTime.UtcNow);
        Assert.Equal("mastered", result.Value.ReviewStatus);
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
                ReviewCount = 1,
                CorrectReviews = 1,
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
        Assert.Equal(1, result.Value.DueCount);
        Assert.Equal(1, result.Value.OverdueCount);
        Assert.Equal(1, result.Value.NewCount);
        Assert.Equal(1, result.Value.MasteredCount);
        Assert.Equal(0, result.Value.LearningCount);
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
        Assert.False(result.Value.IsMastered);
        Assert.True(result.Value.LastReviewWasEarly == false);
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
        Assert.True(result.Value.LastReviewWasEarly == true);
        Assert.Equal("learning", result.Value.ReviewStatus);
    }

    [Fact]
    public async Task GetReviewQueueAsync_ReturnsDueItemsWithQueueState()
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
                NextReviewAt = DateTime.UtcNow.AddHours(-2),
                ReviewCount = 1
            },
            new UserVocabulary
            {
                UserId = _userId,
                LessonId = lesson.Id,
                Topic = "travel",
                Word = "hotel",
                NormalizedWord = "hotel",
                NextReviewAt = DateTime.UtcNow.AddHours(10),
                ReviewCount = 0
            });

        await context.SaveChangesAsync();

        var service = new VocabularyService(context);
        var result = await service.GetReviewQueueAsync(_userId, new VocabularyQueryRequest());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("airport", result.Value.Items[0].Word);
        Assert.True(result.Value.Items[0].IsDue);
        Assert.Equal("overdue", result.Value.Items[0].ReviewStatus);
        Assert.Equal(1, result.Value.OverdueCount);
        Assert.Equal(0, result.Value.NewCount);
    }
}