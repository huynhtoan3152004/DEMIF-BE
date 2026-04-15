using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Payments;

public class GetAdminPaymentAnalyticsService
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public GetAdminPaymentAnalyticsService(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<PaymentAnalyticsStats>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<PaymentAnalyticsStats>(result.Error);
        }

        return Result.Success(result.Value.Payments);
    }
}