using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Users;

public class GetAdminUserAnalyticsService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminUserAnalyticsService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<UserAnalyticsStats>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<UserAnalyticsStats>(result.Error);
        }

        return Result.Success(result.Value.Users);
    }
}