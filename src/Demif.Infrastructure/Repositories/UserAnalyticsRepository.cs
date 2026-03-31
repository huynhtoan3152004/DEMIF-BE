using System;
using System.Threading;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;using Demif.Infrastructure.Persistence;

namespace Demif.Infrastructure.Repositories
{
    public class UserAnalyticsRepository : IUserAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public UserAnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserAnalytics?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.UserAnalytics.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task AddOrUpdateAsync(UserAnalytics analytics, CancellationToken cancellationToken)
        {
            var existing = await _context.UserAnalytics.FirstOrDefaultAsync(x => x.UserId == analytics.UserId, cancellationToken);
            if (existing == null)
            {
                await _context.UserAnalytics.AddAsync(analytics, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(analytics);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
