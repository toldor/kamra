using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;

namespace KamraApp.Application.ShoppingList;

// US-5, M13, ADR-0008 4. (D1, D5): the suggestion goes on the list with the given amount, or without one. The
// same decision again returns the row (200); the opposite one is SHOPPING_SUGGESTION_STATE_CONFLICT.
public sealed class AcceptShoppingSuggestion(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public Task<ShoppingListItemResponse> ExecuteAsync(Guid id, AcceptShoppingSuggestionRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(shopping, ingredients, locks, household, time)}");
}
