using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Demif.Application.Features.Me.Vocabulary;

public class VocabularyService
{
    private static readonly int[] ReviewIntervalsInDays = [1, 3, 7, 14, 30];
    private static readonly string[] SentenceSeparators = [". ", "! ", "? ", "\n"];
    private static readonly Regex WordRegex = new(@"[A-Za-zÀ-ỹ']+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "that", "this", "from", "have", "has", "are", "was", "were",
        "you", "your", "our", "their", "they", "them", "there", "here", "when", "what", "where",
        "which", "will", "would", "could", "should", "about", "into", "over", "under", "then", "than",
        "also", "just", "like", "been", "being", "very", "more", "most", "such", "some", "each",
        "many", "much", "who", "whom", "why", "how", "all", "any", "but", "not", "nor", "too",
        "can", "may", "might", "must", "shall", "hello", "hi", "yes", "no", "ok"
    };

    private readonly IApplicationDbContext _dbContext;

    public VocabularyService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<VocabularyItemResponse>> SaveAsync(
        Guid userId,
        SaveVocabularyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.LessonId == Guid.Empty)
            return Result.Failure<VocabularyItemResponse>(Error.Validation("LessonId is required."));

        if (string.IsNullOrWhiteSpace(request.Word))
            return Result.Failure<VocabularyItemResponse>(Error.Validation("Word is required."));

        var word = request.Word.Trim();

        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LessonId, cancellationToken);

        if (lesson is null || lesson.Status != "published")
            return Result.Failure<VocabularyItemResponse>(Error.NotFound("Không tìm thấy bài học."));

        var topic = string.IsNullOrWhiteSpace(request.Topic)
            ? (lesson.Category ?? lesson.Title)
            : request.Topic.Trim();

        var normalizedWord = NormalizeWord(word);
        if (string.IsNullOrWhiteSpace(normalizedWord))
            return Result.Failure<VocabularyItemResponse>(Error.Validation("Word is required."));

        var now = DateTime.UtcNow;
        var existing = await _dbContext.UserVocabularies
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                  && x.LessonId == request.LessonId
                  && x.Topic == topic
                  && x.NormalizedWord == normalizedWord,
                cancellationToken);

        UserVocabulary item;
        if (existing is null)
        {
            item = new UserVocabulary
            {
                UserId = userId,
                LessonId = request.LessonId,
                Topic = topic,
                Word = word,
                NormalizedWord = normalizedWord,
                Meaning = NormalizeOptional(request.Meaning),
                ContextSentence = NormalizeOptional(request.ContextSentence),
                Note = NormalizeOptional(request.Note),
                ReviewCount = 0,
                CorrectReviews = 0,
                IsMastered = false,
                NextReviewAt = now.AddDays(1)
            };

            _dbContext.UserVocabularies.Add(item);
        }
        else
        {
            existing.Topic = topic;
            existing.Word = word;
            existing.NormalizedWord = normalizedWord;
            existing.Meaning = NormalizeOptional(request.Meaning);
            existing.ContextSentence = NormalizeOptional(request.ContextSentence);
            existing.Note = NormalizeOptional(request.Note);
            existing.UpdatedAt = now;
            item = existing;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(item, lesson));
    }

    public async Task<Result<VocabularyListResponse>> GetAsync(
        Guid userId,
        VocabularyQueryRequest request,
        bool dueOnly = false,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var now = DateTime.UtcNow;

        var query = _dbContext.UserVocabularies
            .AsNoTracking()
            .Include(x => x.Lesson)
            .Where(x => x.UserId == userId);

        if (request.LessonId.HasValue)
            query = query.Where(x => x.LessonId == request.LessonId.Value);

        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            var topic = request.Topic.Trim();
            query = query.Where(x => x.Topic == topic);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Word.Contains(search) || (x.Meaning != null && x.Meaning.Contains(search)) || x.Topic.Contains(search));
        }

        if (dueOnly)
            query = query.Where(x => x.NextReviewAt == null || x.NextReviewAt <= now);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.NextReviewAt ?? now)
            .ThenBy(x => x.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mappedItems = items.Select(item => Map(item)).ToList();

        return Result.Success(new VocabularyListResponse
        {
            Items = mappedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<VocabularyOverviewResponse>> GetOverviewAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var baseQuery = _dbContext.UserVocabularies
            .AsNoTracking()
            .Include(x => x.Lesson)
            .Where(x => x.UserId == userId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var dueCount = await baseQuery.CountAsync(x => x.NextReviewAt == null || x.NextReviewAt <= now, cancellationToken);
        var masteredCount = await baseQuery.CountAsync(x => x.IsMastered, cancellationToken);
        var learningCount = Math.Max(0, totalCount - masteredCount);
        var topicCount = await baseQuery.Select(x => x.Topic).Distinct().CountAsync(cancellationToken);
        var lessonCount = await baseQuery.Select(x => x.LessonId).Distinct().CountAsync(cancellationToken);
        var recentCount = await baseQuery.CountAsync(x => x.CreatedAt >= weekAgo, cancellationToken);

        var recentItems = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Result.Success(new VocabularyOverviewResponse
        {
            TotalCount = totalCount,
            DueCount = dueCount,
            MasteredCount = masteredCount,
            LearningCount = learningCount,
            TopicCount = topicCount,
            LessonCount = lessonCount,
            RecentCount = recentCount,
            MasteryRate = totalCount > 0 ? Math.Round((decimal)masteredCount / totalCount * 100, 1) : 0,
            RecentItems = recentItems.Select(item => Map(item)).ToList()
        });
    }

    public async Task<Result<VocabularySuggestionResponse>> GetSuggestionsAsync(
        Guid userId,
        Guid lessonId,
        VocabularySuggestionQuery request,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken);

        if (lesson is null || lesson.Status != "published")
            return Result.Failure<VocabularySuggestionResponse>(Error.NotFound("Không tìm thấy bài học."));

        var sourceText = BuildTranscriptSource(lesson);
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return Result.Failure<VocabularySuggestionResponse>(
                Error.NotFound("Bài học chưa có transcript để gợi ý từ vựng."));
        }

        var limit = Math.Clamp(request.Limit, 1, 50);
        var existingWords = await _dbContext.UserVocabularies
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.LessonId == lessonId)
            .Select(x => x.NormalizedWord)
            .ToListAsync(cancellationToken);

        var existingSet = existingWords.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sentences = SplitSentences(sourceText);
        var suggestionMap = new Dictionary<string, VocabularySuggestionItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var sentence in sentences)
        {
            foreach (Match match in WordRegex.Matches(sentence))
            {
                var word = match.Value.Trim();
                var normalized = NormalizeWord(word);
                if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 3 || StopWords.Contains(normalized))
                    continue;

                if (suggestionMap.TryGetValue(normalized, out var existing))
                {
                    existing.Frequency++;
                    if (string.IsNullOrWhiteSpace(existing.ContextSentence))
                        existing.ContextSentence = sentence.Trim();
                    continue;
                }

                suggestionMap[normalized] = new VocabularySuggestionItem
                {
                    Word = word,
                    NormalizedWord = normalized,
                    Frequency = 1,
                    ContextSentence = sentence.Trim(),
                    Topic = string.IsNullOrWhiteSpace(lesson.Category) ? lesson.Title : lesson.Category,
                    LessonId = lesson.Id,
                    LessonTitle = lesson.Title,
                    LessonCategory = lesson.Category,
                    IsAlreadySaved = existingSet.Contains(normalized)
                };
            }
        }

        var rankedSuggestions = suggestionMap.Values.Where(x => x.Frequency >= 2).ToList();
        if (rankedSuggestions.Count == 0)
            rankedSuggestions = suggestionMap.Values.ToList();

        var items = rankedSuggestions
            .OrderByDescending(x => x.Frequency)
            .ThenBy(x => x.Word)
            .Take(limit)
            .ToList();

        return Result.Success(new VocabularySuggestionResponse
        {
            LessonId = lesson.Id,
            LessonTitle = lesson.Title,
            LessonCategory = lesson.Category,
            Topic = string.IsNullOrWhiteSpace(lesson.Category) ? lesson.Title : lesson.Category,
            SourceTextLength = sourceText.Length,
            TotalCandidates = suggestionMap.Count,
            Items = items
        });
    }

    public async Task<Result<VocabularyItemResponse>> ReviewAsync(
        Guid userId,
        Guid vocabularyId,
        ReviewVocabularyRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.UserVocabularies
            .Include(x => x.Lesson)
            .FirstOrDefaultAsync(x => x.Id == vocabularyId && x.UserId == userId, cancellationToken);

        if (item is null)
            return Result.Failure<VocabularyItemResponse>(Error.NotFound("Không tìm thấy từ vựng."));

        var now = DateTime.UtcNow;

        item.ReviewCount++;
        if (request.IsCorrect)
            item.CorrectReviews++;

        item.LastReviewedAt = now;
        item.NextReviewAt = now.AddDays(GetNextReviewIntervalDays(item.ReviewCount, request.IsCorrect));
        item.IsMastered = request.IsCorrect && item.CorrectReviews >= 3;
        item.MasteredAt = item.IsMastered && item.MasteredAt is null ? now : item.MasteredAt;
        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(item));
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid vocabularyId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.UserVocabularies
            .FirstOrDefaultAsync(x => x.Id == vocabularyId && x.UserId == userId, cancellationToken);

        if (item is null)
            return Result.Failure(Error.NotFound("Không tìm thấy từ vựng."));

        _dbContext.UserVocabularies.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static int GetNextReviewIntervalDays(int reviewCount, bool isCorrect)
    {
        if (!isCorrect)
            return 1;

        var index = Math.Clamp(reviewCount - 1, 0, ReviewIntervalsInDays.Length - 1);
        return ReviewIntervalsInDays[index];
    }

    private static string NormalizeWord(string input)
    {
        return new string(input
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string BuildTranscriptSource(Lesson lesson)
    {
        if (!string.IsNullOrWhiteSpace(lesson.FullTranscript))
            return lesson.FullTranscript;

        if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            return string.Empty;

        try
        {
            var segments = System.Text.Json.JsonSerializer.Deserialize<List<TimedTranscriptSegment>>(lesson.TimedTranscript, JsonOptions);
            return segments is null || segments.Count == 0
                ? string.Empty
                : string.Join(" ", segments.Select(x => x.Text));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        return text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Split(SentenceSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence));
    }

    private static string? NormalizeOptional(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    private static VocabularyItemResponse Map(UserVocabulary item, Lesson? lesson = null)
    {
        lesson ??= item.Lesson;

        return new VocabularyItemResponse
        {
            Id = item.Id,
            UserId = item.UserId,
            LessonId = item.LessonId,
            LessonTitle = lesson?.Title,
            LessonCategory = lesson?.Category,
            Topic = item.Topic,
            Word = item.Word,
            Meaning = item.Meaning,
            ContextSentence = item.ContextSentence,
            Note = item.Note,
            ReviewCount = item.ReviewCount,
            CorrectReviews = item.CorrectReviews,
            IsMastered = item.IsMastered,
            LastReviewedAt = item.LastReviewedAt,
            NextReviewAt = item.NextReviewAt,
            MasteredAt = item.MasteredAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}

public class SaveVocabularyRequest
{
    public Guid LessonId { get; set; }
    public string Word { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public string? Meaning { get; set; }
    public string? ContextSentence { get; set; }
    public string? Note { get; set; }
}

public class ReviewVocabularyRequest
{
    public bool IsCorrect { get; set; }
}

public class VocabularyQueryRequest
{
    public Guid? LessonId { get; set; }
    public string? Topic { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class VocabularyItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string? LessonTitle { get; set; }
    public string? LessonCategory { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string? Meaning { get; set; }
    public string? ContextSentence { get; set; }
    public string? Note { get; set; }
    public int ReviewCount { get; set; }
    public int CorrectReviews { get; set; }
    public bool IsMastered { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public DateTime? MasteredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VocabularyListResponse
{
    public List<VocabularyItemResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class VocabularyOverviewResponse
{
    public int TotalCount { get; set; }
    public int DueCount { get; set; }
    public int MasteredCount { get; set; }
    public int LearningCount { get; set; }
    public int TopicCount { get; set; }
    public int LessonCount { get; set; }
    public int RecentCount { get; set; }
    public decimal MasteryRate { get; set; }
    public List<VocabularyItemResponse> RecentItems { get; set; } = new();
}

public class VocabularySuggestionQuery
{
    public int Limit { get; set; } = 20;
}

public class VocabularySuggestionResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string? LessonCategory { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int SourceTextLength { get; set; }
    public int TotalCandidates { get; set; }
    public List<VocabularySuggestionItem> Items { get; set; } = new();
}

public class VocabularySuggestionItem
{
    public string Word { get; set; } = string.Empty;
    public string NormalizedWord { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string? LessonCategory { get; set; }
    public string? ContextSentence { get; set; }
    public int Frequency { get; set; }
    public bool IsAlreadySaved { get; set; }
}

internal sealed class TimedTranscriptSegment
{
    public string Text { get; set; } = string.Empty;
}