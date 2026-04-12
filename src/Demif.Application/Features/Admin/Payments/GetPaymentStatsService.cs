using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Admin.Payments;

public class GetPaymentStatsService
{
    private readonly IApplicationDbContext _dbContext;

    public GetPaymentStatsService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentStatsResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var completedPayments = await _dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var totalRevenue = completedPayments.Sum(p => p.Amount);
        
        var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var currentMonthRevenue = completedPayments
            .Where(p => p.CompletedAt >= currentMonthStart)
            .Sum(p => p.Amount);

        return Result.Success(new PaymentStatsResponse
        {
            TotalRevenue = totalRevenue,
            CurrentMonthRevenue = currentMonthRevenue,
            TotalTransactions = completedPayments.Count,
            Currency = "VND"
        });
    }
}

public class PaymentStatsResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public string Currency { get; set; } = "VND";
}
