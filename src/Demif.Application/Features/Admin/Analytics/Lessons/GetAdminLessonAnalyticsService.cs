using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Lessons;

public class GetAdminLessonAnalyticsService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminLessonAnalyticsService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<LessonAnalyticsStats>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<LessonAnalyticsStats>(result.Error);
        }

        return Result.Success(result.Value.Lessons);
    }
}