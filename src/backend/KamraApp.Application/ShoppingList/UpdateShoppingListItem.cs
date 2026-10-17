using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;

namespace KamraApp.Application.ShoppingList;

// US-5: check or uncheck a row and set its amount (D5).
public sealed class UpdateShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household)
{
    public Task<ShoppingListItemResponse> ExecuteAsync(Guid id, UpdateShoppingListItemRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(shopping, ingredients, locks, household)}");
}
