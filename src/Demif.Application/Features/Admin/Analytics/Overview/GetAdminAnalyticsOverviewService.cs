using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Overview;

public class GetAdminAnalyticsOverviewService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminAnalyticsOverviewService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<AdminOverviewResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<AdminOverviewResponse>(result.Error);
        }

        return Result.Success(new AdminOverviewResponse
        {
            GeneratedAt = result.Value.GeneratedAt,
            Summary = result.Value.Summary,
            Alerts = result.Value.Alerts,
            TopUsers = result.Value.TopUsers,
            PopularLessons = result.Value.PopularLessons,
            DifficultLessons = result.Value.DifficultLessons,
            RecentPayments = result.Value.RecentPayments
        });
    }
}