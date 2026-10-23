using KamraApp.Application.Common;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5: not suggested again until the ingredient is back above its condition (scope_contract.md US-5). The same
// decision again changes nothing; the opposite one is SHOPPING_SUGGESTION_STATE_CONFLICT (ADR-0008 4.).
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
        if (suggestion.Reject(time.GetUtcNow()) == SuggestionTransition.Conflict)
        {
            throw ShoppingListErrors.SuggestionStateConflict();
        }

        // Also on a repeat: the save ends the lock's transaction.
        await shopping.SaveChangesAsync(cancellationToken);
    }
}
