using System;
using System.Threading;
using System.Threading.Tasks;
using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories
{
    public interface IUserAnalyticsRepository
    {
        Task<UserAnalytics?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddOrUpdateAsync(UserAnalytics analytics, CancellationToken cancellationToken);
    }
}
