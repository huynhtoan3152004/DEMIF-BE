# DEMIF - Backend Technical Design (C# Only)

## 🎯 Core Problem

```
User nghe audio → Điền vào blanks → Backend check đúng/sai → Trả kết quả
```

---

## 1. Text Comparison Algorithms

### 1.1 Exact Match (Đơn giản nhất)

```csharp
public bool IsCorrect(string userAnswer, string correctAnswer)
{
    return userAnswer.Trim().ToLower() == correctAnswer.Trim().ToLower();
}
```

**Vấn đề:** "hello" vs "helo" → Sai hoàn toàn (dù chỉ thiếu 1 chữ)

---

### 1.2 Levenshtein Distance (Recommended) ⭐

Đo "khoảng cách" giữa 2 string = số thao tác (add/delete/replace) để biến string A thành B.

```csharp
public class TextComparer
{
    /// <summary>
    /// Calculate Levenshtein Distance between two strings
    /// </summary>
    public int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[a.Length, b.Length];
    }

    /// <summary>
    /// Calculate similarity percentage (0-100)
    /// </summary>
    public double CalculateSimilarity(string userAnswer, string correctAnswer)
    {
        var a = userAnswer.Trim().ToLower();
        var b = correctAnswer.Trim().ToLower();

        if (a == b) return 100.0;

        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);

        double similarity = (1.0 - (double)distance / maxLen) * 100;
        return Math.Round(similarity, 1);
    }

    /// <summary>
    /// Check if answer is acceptable (>= threshold)
    /// </summary>
    public bool IsAcceptable(string userAnswer, string correctAnswer, double threshold = 85.0)
    {
        return CalculateSimilarity(userAnswer, correctAnswer) >= threshold;
    }
}
```

**Ví dụ:**
| User Answer | Correct | Distance | Similarity | Result |
|-------------|---------|----------|------------|--------|
| "hello" | "hello" | 0 | 100% | ✅ Correct |
| "helo" | "hello" | 1 | 80% | ⚠️ Close |
| "Sarah" | "Sara" | 1 | 80% | ⚠️ Close |
| "cat" | "dog" | 3 | 0% | ❌ Wrong |

---

### 1.3 Word-by-Word Comparison (Cho Shadowing)

```csharp
public class WordComparison
{
    public ComparisonResult CompareTexts(string original, string userSaid)
    {
        var originalWords = Tokenize(original);
        var userWords = Tokenize(userSaid);
        
        var results = new List<WordResult>();
        int correctCount = 0;
        
        // Use sequence matcher algorithm
        int i = 0, j = 0;
        while (i < originalWords.Count || j < userWords.Count)
        {
            if (i >= originalWords.Count)
            {
                // User said extra words
                results.Add(new WordResult
                {
                    Type = "extra",
                    UserWord = userWords[j],
                    Position = j
                });
                j++;
            }
            else if (j >= userWords.Count)
            {
                // User missed words
                results.Add(new WordResult
                {
                    Type = "missed",
                    ExpectedWord = originalWords[i],
                    Position = i
                });
                i++;
            }
            else if (AreSimilar(originalWords[i], userWords[j]))
            {
                // Correct
                results.Add(new WordResult
                {
                    Type = "correct",
                    ExpectedWord = originalWords[i],
                    UserWord = userWords[j],
                    Position = i
                });
                correctCount++;
                i++; j++;
            }
            else
            {
                // Wrong word
                results.Add(new WordResult
                {
                    Type = "wrong",
                    ExpectedWord = originalWords[i],
                    UserWord = userWords[j],
                    Position = i
                });
                i++; j++;
            }
        }
        
        return new ComparisonResult
        {
            Accuracy = (double)correctCount / originalWords.Count * 100,
            TotalWords = originalWords.Count,
            CorrectWords = correctCount,
            Details = results
        };
    }
    
    private List<string> Tokenize(string text)
    {
        return text.ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
    
    private bool AreSimilar(string a, string b, double threshold = 0.8)
    {
        var comparer = new TextComparer();
        return comparer.CalculateSimilarity(a, b) >= threshold * 100;
    }
}
```

---

## 2. Dictation Scoring Service

### 2.1 Complete Implementation

```csharp
public class DictationScoringService
{
    private readonly TextComparer _comparer = new();
    private const double SIMILARITY_THRESHOLD = 85.0; // 85% để chấp nhận

    public DictationResult Score(DictationSubmission submission, Lesson lesson)
    {
        // 1. Parse lesson template
        var template = JsonSerializer.Deserialize<DictationTemplate>(lesson.DictationTemplate);
        var blanks = template.Blanks;
        
        // 2. Compare each blank
        var results = new List<BlankResult>();
        int correctCount = 0;
        
        for (int i = 0; i < blanks.Count; i++)
        {
            var correctAnswer = blanks[i].Answer;
            var userAnswer = submission.Answers.ElementAtOrDefault(i) ?? "";
            
            var similarity = _comparer.CalculateSimilarity(userAnswer, correctAnswer);
            var isCorrect = similarity >= SIMILARITY_THRESHOLD;
            
            if (isCorrect) correctCount++;
            
            results.Add(new BlankResult
            {
                Index = i,
                CorrectAnswer = correctAnswer,
                UserAnswer = userAnswer,
                IsCorrect = isCorrect,
                Similarity = similarity,
                Hint = blanks[i].Hint
            });
        }
        
        // 3. Calculate score
        double baseScore = (double)correctCount / blanks.Count * 100;
        
        // 4. Apply penalties
        int playPenalty = Math.Max(0, (submission.PlaysUsed - 1) * 5); // -5 điểm mỗi lần nghe thêm
        int timePenalty = CalculateTimePenalty(submission.TimeSpentSeconds, lesson.DurationSeconds);
        
        int finalScore = Math.Max(0, (int)baseScore - playPenalty - timePenalty);
        
        // 5. Calculate XP earned
        int xpEarned = CalculateXP(finalScore, lesson.Level);
        
        return new DictationResult
        {
            LessonId = lesson.Id,
            Score = finalScore,
            TotalBlanks = blanks.Count,
            CorrectBlanks = correctCount,
            PlaysUsed = submission.PlaysUsed,
            TimeSpentSeconds = submission.TimeSpentSeconds,
            XPEarned = xpEarned,
            Details = results,
            Passed = finalScore >= 60, // 60% để pass
            Message = GetResultMessage(finalScore)
        };
    }
    
    private int CalculateTimePenalty(int timeSpent, int expectedTime)
    {
        // Không penalty nếu hoàn thành trong expected time
        if (timeSpent <= expectedTime * 2) return 0;
        
        // -1 điểm mỗi 30 giây quá giờ
        int overtime = timeSpent - expectedTime * 2;
        return Math.Min(10, overtime / 30);
    }
    
    private int CalculateXP(int score, string level)
    {
        int baseXP = level switch
        {
            "beginner" => 10,
            "intermediate" => 20,
            "advanced" => 30,
            _ => 10
        };
        
        return (int)(baseXP * score / 100.0);
    }
    
    private string GetResultMessage(int score) => score switch
    {
        >= 90 => "Xuất sắc! 🎉",
        >= 70 => "Tốt lắm! 👍",
        >= 60 => "Đạt yêu cầu ✅",
        _ => "Cần cải thiện 📚"
    };
}
```

---

## 3. API Design

### 3.1 Dictation Submit API

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // JWT required
public class ExercisesController : ControllerBase
{
    private readonly DictationScoringService _scoringService;
    private readonly ILessonRepository _lessonRepo;
    private readonly IUserExerciseRepository _exerciseRepo;
    private readonly IUserProgressService _progressService;
    
    [HttpPost("dictation/submit")]
    public async Task<ActionResult<DictationResult>> SubmitDictation(
        [FromBody] DictationSubmission submission)
    {
        // 1. Validate input
        if (submission.Answers == null || submission.Answers.Count == 0)
            return BadRequest("Answers required");
        
        // 2. Get current user
        var userId = User.GetUserId(); // From JWT
        
        // 3. Get lesson
        var lesson = await _lessonRepo.GetByIdAsync(submission.LessonId);
        if (lesson == null)
            return NotFound("Lesson not found");
        
        // 4. Score the submission
        var result = _scoringService.Score(submission, lesson);
        
        // 5. Save to database
        var exercise = new UserExercise
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LessonId = lesson.Id,
            ExerciseType = "dictation",
            UserInput = JsonSerializer.Serialize(submission.Answers),
            Score = result.Score,
            TimeSpentSeconds = submission.TimeSpentSeconds,
            ResultDetails = JsonSerializer.Serialize(result.Details),
            CompletedAt = DateTime.UtcNow
        };
        await _exerciseRepo.AddAsync(exercise);
        
        // 6. Update user progress
        await _progressService.UpdateAfterExercise(userId, result);
        
        // 7. Return result
        return Ok(result);
    }
}
```

### 3.2 Request/Response Models

```csharp
// Request
public class DictationSubmission
{
    [Required]
    public Guid LessonId { get; set; }
    
    [Required]
    public List<string> Answers { get; set; } = new();
    
    [Range(1, 5)]
    public int PlaysUsed { get; set; } = 1;
    
    [Range(0, 3600)]
    public int TimeSpentSeconds { get; set; }
}

// Response
public class DictationResult
{
    public Guid LessonId { get; set; }
    public int Score { get; set; }
    public int TotalBlanks { get; set; }
    public int CorrectBlanks { get; set; }
    public int PlaysUsed { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int XPEarned { get; set; }
    public bool Passed { get; set; }
    public string Message { get; set; }
    public List<BlankResult> Details { get; set; }
}

public class BlankResult
{
    public int Index { get; set; }
    public string CorrectAnswer { get; set; }
    public string UserAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public double Similarity { get; set; }
    public string Hint { get; set; }
}
```

---

## 4. Security Best Practices

### 4.1 API Security Checklist

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]))
        };
    });

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100; // 100 requests per minute
    });
});
```

### 4.2 Input Validation

```csharp
public class DictationSubmissionValidator : AbstractValidator<DictationSubmission>
{
    public DictationSubmissionValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty();
        RuleFor(x => x.Answers).Must(a => a.Count <= 50)
            .WithMessage("Too many answers");
        RuleFor(x => x.PlaysUsed).InclusiveBetween(1, 5);
        RuleFor(x => x.TimeSpentSeconds).InclusiveBetween(0, 3600);
        
        // Prevent injection
        RuleForEach(x => x.Answers)
            .MaximumLength(100)
            .Matches(@"^[\w\s\.,!?'-]*$");
    }
}
```

### 4.3 Anti-Cheating Measures

```csharp
public class AntiCheatService
{
    public bool ValidateSubmission(DictationSubmission submission, Lesson lesson)
    {
        // 1. Minimum time check (không thể hoàn thành nhanh hơn audio)
        if (submission.TimeSpentSeconds < lesson.DurationSeconds * 0.5)
        {
            _logger.LogWarning("Suspicious: Too fast completion");
            return false;
        }
        
        // 2. Check if user actually loaded the lesson recently
        var lessonAccess = _cache.Get($"lesson_access:{userId}:{lessonId}");
        if (lessonAccess == null)
        {
            _logger.LogWarning("Suspicious: Lesson not loaded");
            return false;
        }
        
        // 3. Rate limit: max 5 submissions per lesson per hour
        var submissionCount = _cache.Increment($"submission_count:{userId}:{lessonId}");
        if (submissionCount > 5)
        {
            _logger.LogWarning("Rate limit exceeded");
            return false;
        }
        
        return true;
    }
}
```

---

## 5. Scalability Architecture

```
                    ┌─────────────┐
                    │   NGINX     │
                    │ Load Balancer│
                    └──────┬──────┘
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌────────────┐  ┌────────────┐  ┌────────────┐
    │  API #1    │  │  API #2    │  │  API #3    │
    │ (ASP.NET)  │  │ (ASP.NET)  │  │ (ASP.NET)  │
    └──────┬─────┘  └──────┬─────┘  └──────┬─────┘
           │               │               │
           └───────────────┼───────────────┘
                           ▼
    ┌──────────────────────────────────────────────┐
    │                  REDIS                        │
    │  - Session/JWT tokens                         │
    │  - Rate limiting counters                     │
    │  - Leaderboard cache                          │
    └──────────────────────┬───────────────────────┘
                           ▼
    ┌──────────────────────────────────────────────┐
    │              SQL SERVER                       │
    │  - User data                                  │
    │  - Exercise results                           │
    │  - Lessons                                    │
    └──────────────────────────────────────────────┘
```

---

## 6. Task Breakdown (Jira)

### Sprint 2: Dictation Feature

| Task ID | Task | Size | Hours |
|---------|------|------|-------|
| S2-01 | Implement LevenshteinDistance algorithm | S | 2h |
| S2-02 | Implement TextComparer.CalculateSimilarity | S | 2h |
| S2-03 | Create DictationScoringService | M | 4h |
| S2-04 | Add penalty calculations | S | 2h |
| S2-05 | Create DictationSubmission DTO | S | 1h |
| S2-06 | Create DictationResult DTO | S | 1h |
| S2-07 | Create BlankResult DTO | S | 1h |
| S2-08 | Implement POST /api/exercises/dictation/submit | M | 4h |
| S2-09 | Add input validation (FluentValidation) | S | 2h |
| S2-10 | Add anti-cheat checks | M | 4h |
| S2-11 | Save UserExercise to database | S | 2h |
| S2-12 | Update UserProgress after submit | M | 4h |
| S2-13 | Add rate limiting | S | 2h |
| S2-14 | Write unit tests for scoring | M | 4h |

**Total: ~35 hours (1 week)**
