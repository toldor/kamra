using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5 (and the "Bevásárlólistára" buttons of US-3 and US-4): one row per ingredient; a new amount adds up,
// a checked row starts again (M2, D3, M7). Runs inside the ingredient lock (D1).
public sealed class AddShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(AddShoppingListItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var ingredient = await ingredients.FindVisibleAsync(householdId, valid.IngredientId!.Value, cancellationToken)
            ?? throw PantryErrors.IngredientNotFound();
        var unit = ShoppingListInput.ParseUnit(valid.Unit);
        if (unit is { } given)
        {
            PantryInput.EnsureUnitFits(given, ingredient.Dimension, ingredient.Name);
        }

        await locks.LockAsync(householdId, [ingredient.Id], cancellationToken);
        var item = await shopping.FindItemByIngredientAsync(householdId, ingredient.Id, cancellationToken);
        if (item is null)
        {
            item = ShoppingListItem.Create(householdId, ingredient.Id, valid.Amount, unit, time.GetUtcNow());
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
