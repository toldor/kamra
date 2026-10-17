using KamraApp.Application.Common;

namespace KamraApp.Infrastructure.Persistence;

// ADR-0008 6. / D6: opens the transaction of the shared DbContext and takes a transaction-scoped advisory lock
// per (household, ingredient), in id order against deadlocks; the next save commits it.
public sealed class IngredientLocks(KamraDbContext db) : IIngredientLocks
{
    public Task LockAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {db}");
}
