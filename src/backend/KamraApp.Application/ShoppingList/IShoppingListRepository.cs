using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

public sealed record ShoppingListEntry(ShoppingListItem Item, string IngredientName);

// An open suggestion with what its reason needs (M12): the ingredient, its minimum and current total (base units).
public sealed record SuggestionEntry(ShoppingSuggestion Suggestion, string IngredientName, Dimension Dimension,
    IngredientMinimum? Minimum, decimal Total);

// Aggregate-level port for the household's shopping list, suggestions and minimums (AGENTS.md): every read
// takes the household; another household's data is missing. Writes run inside IIngredientLocks (ADR-0008 6.).
public interface IShoppingListRepository
{
    Task<IReadOnlyList<ShoppingListEntry>> ListItemsAsync(Guid householdId, CancellationToken cancellationToken);

    // Not tracked: a writer locks the row's ingredient, then reads the row for update (FindItemByIngredientAsync).
    Task<ShoppingListEntry?> FindItemAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // Tracked, for update inside the ingredient lock.
    Task<ShoppingListItem?> FindItemByIngredientAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken);

    void AddItem(ShoppingListItem item);

    void RemoveItem(ShoppingListItem item);

    Task<IReadOnlyList<SuggestionEntry>> ListOpenSuggestionsAsync(Guid householdId, CancellationToken cancellationToken);

    // Not tracked: the writer locks its ingredient first, then reads it again for update.
    Task<ShoppingSuggestion?> FindSuggestionAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // Tracked and read after the lock, so a decision made meanwhile is seen (ADR-0008 4., D1).
    Task<ShoppingSuggestion?> FindSuggestionForUpdateAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // data_model.md: at most one per household and ingredient (Open, or Rejected and not cleared).
    Task<ShoppingSuggestion?> FindBlockingSuggestionAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken);

    void AddSuggestion(ShoppingSuggestion suggestion);

    Task<IngredientMinimum?> FindMinimumAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IngredientMinimum>> ListMinimumsAsync(Guid householdId, CancellationToken cancellationToken);

    void AddMinimum(IngredientMinimum minimum);

    void RemoveMinimum(IngredientMinimum minimum);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
