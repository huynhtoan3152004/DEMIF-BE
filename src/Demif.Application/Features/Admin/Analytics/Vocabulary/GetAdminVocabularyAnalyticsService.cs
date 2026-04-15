using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Vocabulary;

public class GetAdminVocabularyAnalyticsService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminVocabularyAnalyticsService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<VocabularyAnalyticsStats>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<VocabularyAnalyticsStats>(result.Error);
        }

        return Result.Success(result.Value.Vocabulary);
    }
}