using KamraApp.Application.Common;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// One row per ingredient (data_model.md, ShoppingListItems): a new row, or the amount added to the existing one.
// Shared by adding to the list and accepting a suggestion; runs inside the ingredient lock (ADR-0008 6.).
internal static class ShoppingListRows
{
    public static async Task<ShoppingListItem> AddAsync(IShoppingListRepository shopping, Guid householdId, Guid ingredientId, decimal? amount,
        Unit? unit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var item = await shopping.FindItemByIngredientAsync(householdId, ingredientId, cancellationToken);
        if (item is null)
        {
            item = ShoppingListItem.Create(householdId, ingredientId, amount, unit, now);
            shopping.AddItem(item);
        }
        else if (!item.TryAdd(amount, unit))
        {
            throw ValidationException.ForField("amount", ShoppingListInput.LimitMessage);
        }

        return item;
    }
}
