using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;

namespace KamraApp.Application.ShoppingList;

// US-5 (and the "Bevásárlólistára" buttons of US-3 and US-4): one row per ingredient; a new amount adds up,
// a checked row starts again (M2, D3, M7). Runs inside the ingredient lock (D1).
public sealed class AddShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public Task<ShoppingListItemResponse> ExecuteAsync(AddShoppingListItemRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(shopping, ingredients, locks, household, time)}");
}
