using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Pantry;

// Aggregate-level port (AGENTS.md): every method takes the household, and a depleted (0) item counts
// as missing.
public interface IPantryRepository
{
    Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken);

    // ADR-0008: the expected version becomes the concurrency check of the next save.
    Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken);

    void Add(PantryItem item, StockMovement movement);

    void AddMovement(StockMovement movement);

    // Throws PANTRY_ITEM_MODIFIED when a tracked item changed since it was read.
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
