using KamraApp.Application.Common;
using KamraApp.Domain;

namespace KamraApp.Application.ShoppingList;

// US-5: the household's list, in Hungarian alphabetical order (V-33).
public sealed class ListShoppingList(IShoppingListRepository shopping, ICurrentHousehold household)
{
    public async Task<IReadOnlyList<ShoppingListItemResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        [.. (await shopping.ListItemsAsync(household.HouseholdId, cancellationToken))
            .OrderBy(e => e.IngredientName, Collation.Hungarian)
            .Select(e => ShoppingListItemResponse.From(e.Item, e.IngredientName))];
}
