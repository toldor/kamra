namespace KamraApp.Domain.Pantry;

// Fixed list (CONTEXT.md, Kategória); stored as text. Shelf-life days: data_model.md, Kategórialista.
public enum Category
{
    Dairy,
    Cheese,
    Eggs,
    FreshMeatFish,
    ProcessedMeat,
    Vegetables,
    Fruit,
    Bakery,
    DryGoods,
    CannedAndSauces,
    Frozen,
    Beverages,
    PreparedFood,
    Other,
}

public static class Categories
{
    // Null for Other: there is no estimate, so the expiry date is mandatory (US-1).
    public static int? ShelfLifeDays(Category category) => throw new NotImplementedException();

    public static DateOnly? EstimateExpiry(Category category, DateOnly today) => throw new NotImplementedException();
}
