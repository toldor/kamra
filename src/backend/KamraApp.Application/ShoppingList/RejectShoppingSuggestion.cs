using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// US-5, ADR-0008 4. (D1, D5): not suggested again until the ingredient is back above its condition (M3).
public sealed class RejectShoppingSuggestion(IShoppingListRepository shopping, IIngredientLocks locks, ICurrentHousehold household,
    TimeProvider time)
{
    public Task ExecuteAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException($"red phase {(shopping, locks, household, time)}");
}
