using KamraApp.Application.Common;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Unit.Tests;

// ADR-0008 6.: the lock only matters with a database; the API tests check it. Here it records what was locked.
internal sealed class NoIngredientLocks : IIngredientLocks
{
    public List<Guid> Locked { get; } = [];

    public Task LockAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken)
    {
        Locked.AddRange(ingredientIds);
        return Task.CompletedTask;
    }
}

// In-memory shopping list, suggestions and minimums. Every read filters on the household, like the repository
// (QA-1). Names, dimensions and pantry totals for the suggestion list are given by the test.
internal sealed class FakeShopping : IShoppingListRepository
{
    public List<ShoppingListItem> Items { get; } = [];

    public List<ShoppingSuggestion> Suggestions { get; } = [];

    public List<IngredientMinimum> Minimums { get; } = [];

    public Dictionary<Guid, (string Name, Dimension Dimension)> Ingredients { get; } = [];

    public Dictionary<Guid, decimal> Totals { get; } = [];

    // Recipe names by cooking id, for a suggestion made by a cooking.
    public Dictionary<Guid, string> CookingRecipes { get; } = [];

    public int Saves { get; private set; }

    // The single minimum of the evaluator tests.
    public IngredientMinimum? Minimum
    {
        get => Minimums.SingleOrDefault();
        set
        {
            Minimums.Clear();
            if (value is not null)
            {
                Minimums.Add(value);
            }
        }
    }

    public Task<IReadOnlyList<ShoppingListEntry>> ListItemsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ShoppingListEntry>>([.. Items.Where(i => i.HouseholdId == householdId)
            .Select(i => new ShoppingListEntry(i, Ingredients[i.IngredientId].Name))]);

    public Task<ShoppingListEntry?> FindItemAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(i => i.Id == id && i.HouseholdId == householdId) is { } item
            ? new ShoppingListEntry(item, Ingredients[item.IngredientId].Name)
            : null);

    public Task<ShoppingListItem?> FindItemByIngredientAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(i => i.HouseholdId == householdId && i.IngredientId == ingredientId));

    public void AddItem(ShoppingListItem item) => Items.Add(item);

    public void RemoveItem(ShoppingListItem item) => Items.Remove(item);

    public Task<IReadOnlyList<SuggestionEntry>> ListOpenSuggestionsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SuggestionEntry>>([.. Suggestions
            .Where(s => s.HouseholdId == householdId && s.State == SuggestionState.Open)
            .Select(s => new SuggestionEntry(s, Ingredients[s.IngredientId].Name, Ingredients[s.IngredientId].Dimension,
                s.CookingId is { } cookingId ? CookingRecipes[cookingId] : null,
                Minimums.SingleOrDefault(m => m.HouseholdId == householdId && m.IngredientId == s.IngredientId),
                Totals.GetValueOrDefault(s.IngredientId)))]);

    public Task<ShoppingSuggestion?> FindSuggestionAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Suggestions.SingleOrDefault(s => s.Id == id && s.HouseholdId == householdId));

    public Task<ShoppingSuggestion?> FindSuggestionForUpdateAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        FindSuggestionAsync(householdId, id, cancellationToken);

    public Task<ShoppingSuggestion?> FindBlockingSuggestionAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        Task.FromResult(Suggestions.SingleOrDefault(s => s.HouseholdId == householdId && s.IngredientId == ingredientId && s.Blocks));

    public void AddSuggestion(ShoppingSuggestion suggestion) => Suggestions.Add(suggestion);

    public Task<IngredientMinimum?> FindMinimumAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        Task.FromResult(Minimums.SingleOrDefault(m => m.HouseholdId == householdId && m.IngredientId == ingredientId));

    public Task<IReadOnlyList<IngredientMinimum>> ListMinimumsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IngredientMinimum>>([.. Minimums.Where(m => m.HouseholdId == householdId)]);

    public void AddMinimum(IngredientMinimum minimum) => Minimums.Add(minimum);

    public void RemoveMinimum(IngredientMinimum minimum) => Minimums.Remove(minimum);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
