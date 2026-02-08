using System.Text.Json;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Subscriptions.GetPlans;

/// <summary>
/// GetPlans Service - lấy danh sách gói subscription đang active
/// </summary>
public class GetPlansService
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetPlansService(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<GetPlansResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _planRepository.GetActivePlansAsync(cancellationToken);

        var response = new GetPlansResponse
        {
            Plans = plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Tier = p.Tier.ToString(),
                Price = p.Price,
                Currency = p.Currency,
                BillingCycle = p.BillingCycle.ToString(),
                DurationDays = p.DurationDays,
                Features = ParseFeatures(p.Features),
                BadgeText = p.BadgeText,
                BadgeColor = p.BadgeColor
            }).ToList()
        };

        return Result.Success(response);
    }

    private static List<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrEmpty(featuresJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
