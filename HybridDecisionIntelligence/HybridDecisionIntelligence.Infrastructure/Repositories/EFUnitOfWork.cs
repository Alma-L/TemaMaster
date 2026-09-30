using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HybridDecisionIntelligence.Infrastructure.Repositories
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly HybridDecisionContext _context;

        public EFUnitOfWork(HybridDecisionContext context)
        {
            _context = context;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default)
        {
            // Repositories share this scoped DbContext, so their SaveChangesAsync calls
            // all enlist in the transaction below. Disposing without Commit rolls back.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                var result = await work();
                await transaction.CommitAsync(cancellationToken);
                return result;
            });
        }
    }
}
