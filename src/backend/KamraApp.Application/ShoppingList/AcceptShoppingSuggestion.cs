using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5: the suggestion goes on the list with the given amount, or without one. Answered with 204 like a rejection,
// so a repeated acceptance gets the same answer even after the row changed; the opposite decision is
// SHOPPING_SUGGESTION_STATE_CONFLICT (ADR-0008 4.). The suggestion is read again inside the lock.
public sealed class AcceptShoppingSuggestion(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public async Task ExecuteAsync(Guid id, AcceptShoppingSuggestionRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var found = await shopping.FindSuggestionAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.SuggestionNotFound();
        var ingredient = await ingredients.FindVisibleAsync(householdId, found.IngredientId, cancellationToken)
            ?? throw PantryErrors.IngredientNotFound();
        var unit = ShoppingListInput.UnitFor(ingredient, valid.Unit);

        await locks.LockAsync(householdId, [ingredient.Id], cancellationToken);
        var suggestion = await shopping.FindSuggestionForUpdateAsync(householdId, id, cancellationToken)
            ?? throw ShoppingListErrors.SuggestionNotFound();
        var now = time.GetUtcNow();
        switch (suggestion.Accept(now))
        {
            case SuggestionTransition.Conflict:
                throw ShoppingListErrors.SuggestionStateConflict();
            case SuggestionTransition.Changed:
                await ShoppingListRows.AddAsync(shopping, householdId, ingredient.Id, valid.Amount, unit, now, cancellationToken);
                break;
        }

        // Also on a repeat: the save ends the lock's transaction.
        await shopping.SaveChangesAsync(cancellationToken);
    }
}
