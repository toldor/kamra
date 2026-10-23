using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.ShoppingList;

// US-5: check or uncheck a row; a given amount replaces the row's, a missing one keeps it (api.md). The row is
// read again inside the lock, so an amount added meanwhile is not lost by an older copy.
public sealed class UpdateShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household)
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(Guid id, UpdateShoppingListItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var entry = await shopping.FindItemAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.ItemNotFound();
        Domain.Quantities.Unit? unit = null;
        if (valid.Unit is not null)
        {
            var ingredient = await ingredients.FindVisibleAsync(householdId, entry.Item.IngredientId, cancellationToken)
                ?? throw PantryErrors.IngredientNotFound();
            unit = ShoppingListInput.UnitFor(ingredient, valid.Unit);
        }

        await locks.LockAsync(householdId, [entry.Item.IngredientId], cancellationToken);
        var item = await shopping.FindItemByIngredientAsync(householdId, entry.Item.IngredientId, cancellationToken);
        if (item?.Id != id)
        {
            throw ShoppingListErrors.ItemNotFound();
        }

        item.Update(valid.Checked!.Value, valid.Amount, unit);
        await shopping.SaveChangesAsync(cancellationToken);
        return ShoppingListItemResponse.From(item, entry.IngredientName);
    }
}
