using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Infrastructure.Persistence;

// Every query filters on the household (QA-1). Mapped with the AddShoppingList migration (D7).
public sealed class ShoppingListRepository(KamraDbContext db) : IShoppingListRepository
{
    public Task<IReadOnlyList<ShoppingListEntry>> ListItemsAsync(Guid householdId, CancellationToken cancellationToken) => throw RedPhase();

    public Task<ShoppingListEntry?> FindItemAsync(Guid householdId, Guid id, CancellationToken cancellationToken) => throw RedPhase();

    public Task<ShoppingListItem?> FindItemByIngredientAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        throw RedPhase();

    public void AddItem(ShoppingListItem item) => throw RedPhase();

    public void RemoveItem(ShoppingListItem item) => throw RedPhase();

    public Task<IReadOnlyList<SuggestionEntry>> ListOpenSuggestionsAsync(Guid householdId, CancellationToken cancellationToken) =>
        throw RedPhase();

    public Task<ShoppingSuggestion?> FindSuggestionAsync(Guid householdId, Guid id, CancellationToken cancellationToken) => throw RedPhase();

    public Task<ShoppingSuggestion?> FindBlockingSuggestionAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        throw RedPhase();

    public void AddSuggestion(ShoppingSuggestion suggestion) => throw RedPhase();

    public Task<IngredientMinimum?> FindMinimumAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        throw RedPhase();

    public Task<IReadOnlyList<IngredientMinimum>> ListMinimumsAsync(Guid householdId, CancellationToken cancellationToken) =>
        throw RedPhase();

    public void AddMinimum(IngredientMinimum minimum) => throw RedPhase();

    public void RemoveMinimum(IngredientMinimum minimum) => throw RedPhase();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => throw RedPhase();

    private NotImplementedException RedPhase() => new($"red phase {db}");
}
