using KamraApp.Application.Common;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5, ADR-0008 4. (D1, D5): not suggested again until the ingredient is back above its condition (M3).
public sealed class RejectShoppingSuggestion(IShoppingListRepository shopping, IIngredientLocks locks, ICurrentHousehold household,
    TimeProvider time)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var householdId = household.HouseholdId;
        var found = await shopping.FindSuggestionAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.SuggestionNotFound();
        await locks.LockAsync(householdId, [found.IngredientId], cancellationToken);
        var suggestion = await shopping.FindSuggestionForUpdateAsync(householdId, id, cancellationToken)
            ?? throw ShoppingListErrors.SuggestionNotFound();
        switch (suggestion.Reject(time.GetUtcNow()))
        {
            case SuggestionTransition.Conflict:
                throw ShoppingListErrors.SuggestionStateConflict();
            case SuggestionTransition.Changed:
                await shopping.SaveChangesAsync(cancellationToken);
                break;
        }
    }
}
