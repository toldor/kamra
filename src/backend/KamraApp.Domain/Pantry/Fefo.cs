namespace KamraApp.Domain.Pantry;

// A pantry item as the rules see it: how much of which ingredient, until when.
public sealed record StockItem(Guid PantryItemId, Guid IngredientId, decimal Quantity, DateOnly ExpiryDate);

// US-4 FEFO: an amount is taken from the earliest expiring items first (expired ones included, Q5);
// equal expiry dates are split by item id, so the result is always the same.
public static class Fefo
{
    // At most the available amount is allocated; the caller compares the total with what it needed.
    public static IReadOnlyList<(StockItem Item, decimal Amount)> Allocate(IEnumerable<StockItem> items, decimal amount) =>
        throw new NotImplementedException();
}
