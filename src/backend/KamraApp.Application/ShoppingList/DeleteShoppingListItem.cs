using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// US-5: removes a row; afterwards the ingredient no longer suppresses a suggestion (data_model.md).
public sealed class DeleteShoppingListItem(IShoppingListRepository shopping, IIngredientLocks locks, ICurrentHousehold household)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var householdId = household.HouseholdId;
        var entry = await shopping.FindItemAsync(householdId, id, cancellationToken) ?? throw ShoppingListErrors.ItemNotFound();
        await locks.LockAsync(householdId, [entry.Item.IngredientId], cancellationToken);
        var item = await shopping.FindItemByIngredientAsync(householdId, entry.Item.IngredientId, cancellationToken);
        if (item?.Id != id)
        {
            throw ShoppingListErrors.ItemNotFound();
        }

        shopping.RemoveItem(item);
        await shopping.SaveChangesAsync(cancellationToken);
    }
}
