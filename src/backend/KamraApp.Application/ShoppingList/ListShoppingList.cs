using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// US-5: the household's list, in Hungarian alphabetical order (V-33).
public sealed class ListShoppingList(IShoppingListRepository shopping, ICurrentHousehold household)
{
    public Task<IReadOnlyList<ShoppingListItemResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(shopping, household)}");
}
