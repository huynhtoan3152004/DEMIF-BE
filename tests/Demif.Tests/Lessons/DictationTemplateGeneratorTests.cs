using Demif.Application.Features.Lessons;
using Demif.Domain.Enums;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho DictationTemplateGenerator — test core engine
/// </summary>
public class DictationTemplateGeneratorTests
{
    private const string SampleTranscript =
        "Hello everyone. Welcome to our English class. Today we will learn about business communication. " +
        "It is very important to speak clearly. Practice makes perfect.";
    private const int SampleDuration = 30; // 30 giây

    #region GenerateTimedTranscript Tests

    [Fact]
    public void GenerateTimedTranscript_WithValidInput_ReturnsSegments()
    {
        // Act
        var result = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("[]", result);
        Assert.Contains("startTime", result);
        Assert.Contains("endTime", result);
        Assert.Contains("text", result);
    }

    [Fact]
    public void GenerateTimedTranscript_SplitsCorrectlyBySentence()
    {
        var transcript = "Hello world. How are you? I am fine!";
        var result = DictationTemplateGenerator.GenerateTimedTranscript(transcript, 10);

        // Phải tách thành 3 segments (3 câu: . ? !)
        Assert.Contains("Hello world.", result);
        Assert.Contains("How are you?", result);
        Assert.Contains("I am fine!", result);
    }

    [Fact]
    public void GenerateTimedTranscript_EmptyText_ReturnsEmptyArray()
    {
        var result = DictationTemplateGenerator.GenerateTimedTranscript("", 10);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void GenerateTimedTranscript_WhitespaceOnly_ReturnsEmptyArray()
    {
        var result = DictationTemplateGenerator.GenerateTimedTranscript("   ", 10);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void GenerateTimedTranscript_LastSegment_EndsAtDuration()
    {
        var result = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);

        // Segment cuối phải kết thúc đúng tại DurationSeconds
        Assert.Contains($"\"endTime\":{SampleDuration}", result.Replace(" ", ""));
    }

    [Fact]
    public void GenerateTimedTranscript_NoPunctuation_ChunksByWords()
    {
        // Đoạn text dài không có dấu câu → chia theo ~10 từ
        var longText = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen";
        var result = DictationTemplateGenerator.GenerateTimedTranscript(longText, 20);

        Assert.NotEqual("[]", result);
        Assert.Contains("startTime", result);
    }

    #endregion

    #region GenerateAllTemplates Tests

    [Fact]
    public void GenerateAllTemplates_GeneratesAllFourLevels()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var result = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        Assert.Contains("Beginner", result);
        Assert.Contains("Intermediate", result);
        Assert.Contains("Advanced", result);
        Assert.Contains("Expert", result);
    }

    [Fact]
    public void GenerateAllTemplates_EmptyJson_ReturnsEmptyObject()
    {
        var result = DictationTemplateGenerator.GenerateAllTemplates("[]");
        Assert.Equal("{}", result);
    }

    [Fact]
    public void GenerateAllTemplates_BeginnerHasFewerBlanks_ThanExpert()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var beginner = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Beginner);
        var expert = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Expert);

        Assert.NotNull(beginner);
        Assert.NotNull(expert);
        Assert.True(beginner.TotalBlanks < expert.TotalBlanks,
            $"Beginner blanks ({beginner.TotalBlanks}) should be less than Expert blanks ({expert.TotalBlanks})");
    }

    [Fact]
    public void GenerateAllTemplates_BlankPercentageIsCorrect()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var beginner = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Beginner);
        Assert.NotNull(beginner);
        Assert.Equal(15, beginner.BlankPercentage);

        var intermediate = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Intermediate);
        Assert.NotNull(intermediate);
        Assert.Equal(35, intermediate.BlankPercentage);
    }

    #endregion

    #region GetTemplateForLevel Tests

    [Fact]
    public void GetTemplateForLevel_ValidLevel_ReturnsTemplate()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var result = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Beginner);

        Assert.NotNull(result);
        Assert.Equal("Beginner", result.Level);
        Assert.True(result.TotalBlanks > 0);
        Assert.True(result.TotalWords > 0);
        Assert.NotEmpty(result.Segments);
    }

    [Fact]
    public void GetTemplateForLevel_BlanksHaveHints()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var beginner = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Beginner);
        Assert.NotNull(beginner);

        var blankWords = beginner.Segments
            .SelectMany(s => s.Words)
            .Where(w => w.IsBlank)
            .ToList();

        // Beginner phải có hint (FirstLetterAndLength)
        Assert.All(blankWords, w =>
        {
            Assert.NotNull(w.Hint);
            Assert.NotEmpty(w.Hint);
            Assert.NotNull(w.Answer);
            Assert.True(w.Length > 0);
        });
    }

    [Fact]
    public void GetTemplateForLevel_ExpertHasMinimalHints()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var expert = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Expert);
        Assert.NotNull(expert);

        var blankWords = expert.Segments
            .SelectMany(s => s.Words)
            .Where(w => w.IsBlank)
            .ToList();

        // Expert hint phải là "____" (no info)
        Assert.All(blankWords, w =>
        {
            Assert.Equal("____", w.Hint);
        });
    }

    [Fact]
    public void GetTemplateForLevel_NonBlankWordsHaveText()
    {
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);
        var templates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        var beginner = DictationTemplateGenerator.GetTemplateForLevel(templates, Level.Beginner);
        Assert.NotNull(beginner);

        var nonBlankWords = beginner.Segments
            .SelectMany(s => s.Words)
            .Where(w => !w.IsBlank)
            .ToList();

        Assert.All(nonBlankWords, w =>
        {
            Assert.NotEmpty(w.Text);
            Assert.Null(w.Answer); // Non-blank không có Answer
        });
    }

    [Fact]
    public void GetTemplateForLevel_DeterministicOutput()
    {
        // Cùng input → cùng output (do fixed seed)
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(SampleTranscript, SampleDuration);

        var templates1 = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);
        var templates2 = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        Assert.Equal(templates1, templates2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateTimedTranscript_SingleWord_ReturnsOneSegment()
    {
        var result = DictationTemplateGenerator.GenerateTimedTranscript("Hello", 5);
        Assert.NotEqual("[]", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void GenerateAllTemplates_VeryShortTranscript_HandleGracefully()
    {
        // Chỉ có 2 từ → vẫn generate được template
        var timedTranscript = DictationTemplateGenerator.GenerateTimedTranscript("Hello world.", 2);
        var result = DictationTemplateGenerator.GenerateAllTemplates(timedTranscript);

        Assert.Contains("Beginner", result);
    }

    #endregion
}
