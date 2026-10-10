using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Pantry;

// A pantry item with its ingredient's name, as the repository reads it.
public sealed record PantryEntry(PantryItem Item, string IngredientName);

public sealed record PantryFilter(string? NormalizedSearch, Category? Category, DateOnly? SoonExpiringOn);

// Aggregate-level port (AGENTS.md): every read takes the household, and a depleted (0) item counts as
// missing. Add and AddMovement take no household id: the new item and movement already carry the one
// the use case got from ICurrentHousehold, and a second parameter could only disagree with it.
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
