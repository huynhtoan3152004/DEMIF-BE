using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Content;

public class GetAdminContentAnalyticsService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminContentAnalyticsService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<AdminContentAnalyticsResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<AdminContentAnalyticsResponse>(result.Error);
        }

        return Result.Success(new AdminContentAnalyticsResponse
        {
            Blogs = result.Value.Blogs,
            Notifications = result.Value.Notifications,
            Engagement = result.Value.Engagement
        });
    }
}