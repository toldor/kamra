using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.ShoppingList;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// Every query filters on the household (QA-1). Writers read for update inside the ingredient lock (ADR-0008 6.).
public sealed class ShoppingListRepository(KamraDbContext db) : IShoppingListRepository
{
    public async Task<IReadOnlyList<ShoppingListEntry>> ListItemsAsync(Guid householdId, CancellationToken cancellationToken) =>
        [.. (await (from item in db.ShoppingListItems.AsNoTracking()
                    join ingredient in db.Ingredients on item.IngredientId equals ingredient.Id
                    where item.HouseholdId == householdId
                    select new { Item = item, ingredient.Name })
            .ToListAsync(cancellationToken)).Select(x => new ShoppingListEntry(x.Item, x.Name))];

    public async Task<ShoppingListEntry?> FindItemAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        await (from item in db.ShoppingListItems.AsNoTracking()
               join ingredient in db.Ingredients on item.IngredientId equals ingredient.Id
               where item.Id == id && item.HouseholdId == householdId
               select new ShoppingListEntry(item, ingredient.Name))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<ShoppingListItem?> FindItemByIngredientAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        db.ShoppingListItems.SingleOrDefaultAsync(i => i.HouseholdId == householdId && i.IngredientId == ingredientId, cancellationToken);

    public void AddItem(ShoppingListItem item) => db.ShoppingListItems.Add(item);

    public void RemoveItem(ShoppingListItem item) => db.ShoppingListItems.Remove(item);

    public async Task<IReadOnlyList<SuggestionEntry>> ListOpenSuggestionsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var rows = await (from suggestion in db.ShoppingSuggestions.AsNoTracking()
                          join ingredient in db.Ingredients on suggestion.IngredientId equals ingredient.Id
                          where suggestion.HouseholdId == householdId && suggestion.State == SuggestionState.Open
                          select new
                          {
                              Suggestion = suggestion,
                              ingredient.Name,
                              ingredient.Dimension,
                              RecipeName = (from cooking in db.Cookings
                                            join recipe in db.Recipes on cooking.RecipeId equals recipe.Id
                                            where cooking.Id == suggestion.CookingId
                                            select recipe.Name).FirstOrDefault(),
                              Minimum = db.IngredientMinimums.AsNoTracking()
                                  .SingleOrDefault(m => m.HouseholdId == householdId && m.IngredientId == suggestion.IngredientId),
                              Total = db.PantryItems.Where(i => i.HouseholdId == householdId && i.IngredientId == suggestion.IngredientId)
                                  .Sum(i => (decimal?)i.Quantity) ?? 0,
                          })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(r => new SuggestionEntry(r.Suggestion, r.Name, r.Dimension, r.RecipeName, r.Minimum, r.Total))];
    }

    public Task<ShoppingSuggestion?> FindSuggestionAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        db.ShoppingSuggestions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id && s.HouseholdId == householdId, cancellationToken);

    public Task<ShoppingSuggestion?> FindSuggestionForUpdateAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        db.ShoppingSuggestions.SingleOrDefaultAsync(s => s.Id == id && s.HouseholdId == householdId, cancellationToken);

    public Task<ShoppingSuggestion?> FindBlockingSuggestionAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        db.ShoppingSuggestions.SingleOrDefaultAsync(s => s.HouseholdId == householdId && s.IngredientId == ingredientId
            && (s.State == SuggestionState.Open || (s.State == SuggestionState.Rejected && s.ClearedAt == null)), cancellationToken);

    public void AddSuggestion(ShoppingSuggestion suggestion) => db.ShoppingSuggestions.Add(suggestion);

    public Task<IngredientMinimum?> FindMinimumAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
        db.IngredientMinimums.SingleOrDefaultAsync(m => m.HouseholdId == householdId && m.IngredientId == ingredientId, cancellationToken);

    public async Task<IReadOnlyList<IngredientMinimum>> ListMinimumsAsync(Guid householdId, CancellationToken cancellationToken) =>
        await db.IngredientMinimums.AsNoTracking().Where(m => m.HouseholdId == householdId).ToListAsync(cancellationToken);

    public void AddMinimum(IngredientMinimum minimum) => db.IngredientMinimums.Add(minimum);

    public void RemoveMinimum(IngredientMinimum minimum) => db.IngredientMinimums.Remove(minimum);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAndCommitAsync(cancellationToken);
}
