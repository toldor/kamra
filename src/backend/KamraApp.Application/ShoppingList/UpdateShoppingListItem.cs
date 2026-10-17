using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.ShoppingList;

// US-5: check or uncheck a row and set its amount (D5). The row is read again inside the lock, so an amount
// added meanwhile is not lost by an older copy.
public sealed class UpdateShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household)
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(Guid id, UpdateShoppingListItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var entry = await shopping.FindItemAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.ItemNotFound();
        var unit = ShoppingListInput.ParseUnit(valid.Unit);
        if (unit is { } given)
        {
            var ingredient = await ingredients.FindVisibleAsync(householdId, entry.Item.IngredientId, cancellationToken)
                ?? throw PantryErrors.IngredientNotFound();
            PantryInput.EnsureUnitFits(given, ingredient.Dimension, ingredient.Name);
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
