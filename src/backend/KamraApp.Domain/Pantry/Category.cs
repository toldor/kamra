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
    public static int? ShelfLifeDays(Category category) => category switch
    {
        Category.FreshMeatFish or Category.Bakery => 1,
        Category.Fruit => 2,
        Category.Vegetables or Category.PreparedFood => 3,
        Category.Dairy or Category.ProcessedMeat => 7,
        Category.Cheese or Category.Eggs or Category.Beverages => 21,
        Category.DryGoods or Category.CannedAndSauces or Category.Frozen => 90,
        Category.Other => null,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown category."),
    };

    public static DateOnly? EstimateExpiry(Category category, DateOnly today) =>
        ShelfLifeDays(category) is int days ? today.AddDays(days) : null;
}
