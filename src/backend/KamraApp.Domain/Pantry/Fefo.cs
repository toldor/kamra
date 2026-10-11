namespace KamraApp.Domain.Pantry;

// A pantry item as the rules see it: how much of which ingredient, until when.
public sealed record StockItem(Guid PantryItemId, Guid IngredientId, decimal Quantity, DateOnly ExpiryDate)
{
    public static StockItem From(PantryItem item) => new(item.Id, item.IngredientId, item.Quantity, item.ExpiryDate);
}

// US-4 FEFO: an amount is taken from the earliest expiring items first (expired ones included, Q5);
// equal expiry dates are split by item id, so the result is always the same.
public static class Fefo
{
    // At most the available amount is allocated; the caller compares the total with what it needed.
    public static IReadOnlyList<(StockItem Item, decimal Amount)> Allocate(IEnumerable<StockItem> items, decimal amount)
    {
        var allocations = new List<(StockItem Item, decimal Amount)>();
        var remaining = amount;
        foreach (var item in items.OrderBy(i => i.ExpiryDate).ThenBy(i => i.PantryItemId))
        {
            if (remaining <= 0)
            {
                break;
            }

            var taken = Math.Min(item.Quantity, remaining);
            allocations.Add((item, taken));
            remaining -= taken;
        }

        return allocations;
    }
}
