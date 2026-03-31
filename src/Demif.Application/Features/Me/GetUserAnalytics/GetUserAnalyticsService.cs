using System;
using System.Threading;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Me.GetUserAnalytics
{
    public class GetUserAnalyticsService
    {
        private readonly IUserAnalyticsRepository _analyticsRepository;

        public GetUserAnalyticsService(IUserAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }

        public async Task<UserAnalytics?> GetAnalyticsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _analyticsRepository.GetByUserIdAsync(userId, cancellationToken);
        }
    }
}
