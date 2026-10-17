using KamraApp.Domain.Pantry;

namespace KamraApp.Application.ShoppingList;

// US-5: what a change of an ingredient's total means for the shopping suggestions. Called inside the ingredient
// lock (ADR-0008 6.), before the caller saves, so the suggestion is saved with the change. A rejected suggestion
// back above its condition is cleared first (M3); a new one is made only when nothing blocks it and the
// ingredient is not on the list unchecked (M15).
public sealed class SuggestionEvaluator(IShoppingListRepository shopping)
{
    // True when a new suggestion was made (M5: the cooking's summary names it).
    public Task<bool> AfterQuantityChangeAsync(Guid householdId, Guid ingredientId, decimal before, decimal after, MovementReason reason,
        DateTimeOffset now, CancellationToken cancellationToken) => throw new NotImplementedException($"red phase {shopping}");

    // M1, M3: after a minimum was set (or removed, null).
    public Task AfterMinimumChangeAsync(Guid householdId, Guid ingredientId, decimal total, decimal? minimum, DateTimeOffset now,
        CancellationToken cancellationToken) => throw new NotImplementedException($"red phase {shopping}");
}
