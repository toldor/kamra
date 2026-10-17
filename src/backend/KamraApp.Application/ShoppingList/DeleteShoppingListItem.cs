using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// US-5: removes a row; afterwards the ingredient no longer suppresses a suggestion (M15).
public sealed class DeleteShoppingListItem(IShoppingListRepository shopping, IIngredientLocks locks, ICurrentHousehold household)
{
    public Task ExecuteAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException($"red phase {(shopping, locks, household)}");
}
