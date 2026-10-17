using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// US-5, M12, M13: the open suggestions with their reason and the amount to buy, oldest first.
public sealed class ListShoppingSuggestions(IShoppingListRepository shopping, ICurrentHousehold household)
{
    public Task<IReadOnlyList<ShoppingSuggestionResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(shopping, household)}");
}
