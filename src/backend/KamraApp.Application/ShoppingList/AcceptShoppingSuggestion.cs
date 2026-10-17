using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5, M13, ADR-0008 4. (D1, D5): the suggestion goes on the list with the given amount, or without one. The
// same decision again returns the row (200); the opposite one is SHOPPING_SUGGESTION_STATE_CONFLICT.
public sealed class AcceptShoppingSuggestion(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(Guid id, AcceptShoppingSuggestionRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var found = await shopping.FindSuggestionAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.SuggestionNotFound();
        var ingredient = await ingredients.FindVisibleAsync(householdId, found.IngredientId, cancellationToken)
            ?? throw PantryErrors.IngredientNotFound();
        var unit = ShoppingListInput.ParseUnit(valid.Unit);
        if (unit is { } given)
        {
            PantryInput.EnsureUnitFits(given, ingredient.Dimension, ingredient.Name);
        }

        await locks.LockAsync(householdId, [ingredient.Id], cancellationToken);
        var suggestion = await shopping.FindSuggestionForUpdateAsync(householdId, id, cancellationToken)
            ?? throw ShoppingListErrors.SuggestionNotFound();
        var now = time.GetUtcNow();
        var transition = suggestion.Accept(now);
        if (transition == SuggestionTransition.Conflict)
        {
            throw ShoppingListErrors.SuggestionStateConflict();
        }

        var item = await shopping.FindItemByIngredientAsync(householdId, ingredient.Id, cancellationToken);
        if (transition == SuggestionTransition.Repeated)
        {
            // The first acceptance already put it on the list; the repeat changes nothing.
            return item is null ? throw ShoppingListErrors.ItemNotFound() : ShoppingListItemResponse.From(item, ingredient.Name);
        }

        if (item is null)
        {
            item = ShoppingListItem.Create(householdId, ingredient.Id, valid.Amount, unit, now);
            shopping.AddItem(item);
        }
        else if (!item.TryAdd(valid.Amount, unit))
        {
            throw ValidationException.ForField("amount", ShoppingListInput.LimitMessage);
        }

        await shopping.SaveChangesAsync(cancellationToken);
        return ShoppingListItemResponse.From(item, ingredient.Name);
    }
}
