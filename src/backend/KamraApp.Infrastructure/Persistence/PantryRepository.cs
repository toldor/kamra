using KamraApp.Application.Pantry;
using KamraApp.Domain.Pantry;

namespace KamraApp.Infrastructure.Persistence;

public sealed class PantryRepository(KamraDbContext db) : IPantryRepository
{
    public Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {db.GetHashCode()}");

    public Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public void Add(PantryItem item, StockMovement movement) => throw new NotImplementedException();

    public void AddMovement(StockMovement movement) => throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
