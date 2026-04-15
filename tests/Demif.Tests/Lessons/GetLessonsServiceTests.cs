using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Lessons.GetLessons;
using Demif.Domain.Entities;
using Moq;

namespace Demif.Tests.Lessons;

public class GetLessonsServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly GetLessonsService _service;

    public GetLessonsServiceTests()
    {
        _cacheMock
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<GetLessonsResponse>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<CancellationToken, Task<GetLessonsResponse>> factory, TimeSpan? expiration, CancellationToken token) => await factory(token));

        _service = new GetLessonsService(_lessonRepoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_PassesMediaTagAndSearchFiltersToRepository()
    {
        _lessonRepoMock
            .Setup(r => r.GetForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Lesson> Items, int TotalCount))(Array.Empty<Lesson>(), 0));

        var request = new GetLessonsRequest
        {
            Page = 1,
            PageSize = 10,
            Level = "Beginner",
            Type = "Dictation",
            Category = "business",
            MediaType = "audio",
            Tag = "bbc",
            Search = "english"
        };

        var result = await _service.ExecuteAsync(request, userId: null);

        Assert.True(result.IsSuccess);
        _lessonRepoMock.Verify(r => r.GetForUserAsync(
            1,
            10,
            false,
            "Beginner",
            "Dictation",
            "business",
            "audio",
            "bbc",
            "english",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NormalizesNumericStringFiltersToCanonicalNames()
    {
        _lessonRepoMock
            .Setup(r => r.GetForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Lesson> Items, int TotalCount))(Array.Empty<Lesson>(), 0));

        var request = new GetLessonsRequest
        {
            Page = 1,
            PageSize = 10,
            Level = "3",
            Type = "1"
        };

        var result = await _service.ExecuteAsync(request, userId: null);

        Assert.True(result.IsSuccess);
        _lessonRepoMock.Verify(r => r.GetForUserAsync(
            1,
            10,
            false,
            "Expert",
            "Shadowing",
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}