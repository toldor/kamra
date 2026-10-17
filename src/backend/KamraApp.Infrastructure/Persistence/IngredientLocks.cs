using KamraApp.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// ADR-0008 6. / D6: opens the transaction of the shared DbContext and takes a transaction-scoped advisory lock
// per (household, ingredient), in id order against deadlocks; the next save commits it (UnitOfWork). The key is
// a 64-bit hash of the pair: a collision only makes two ingredients wait for each other.
public sealed class IngredientLocks(KamraDbContext db) : IIngredientLocks
{
    public async Task LockAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        foreach (var ingredientId in ingredientIds.Distinct().Order())
        {
            var key = $"{householdId}:{ingredientId}";
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        }
    }
}
