using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Pantry;

// CONTEXT.md, Készlettétel. The quantity changes only together with a stock movement, so the
// quantity always equals the sum of the item's movements (data_model.md).
public sealed class PantryItem
{
    // The column is numeric(12,3): PostgreSQL would round finer amounts and reject larger ones, so the
    // stored quantity and log would drift from the values in memory.
    private const decimal MaxQuantity = 999_999_999.999m;

    private PantryItem(Guid id, Guid householdId, Guid ingredientId, decimal quantity, Unit enteredUnit, Category category,
        DateOnly expiryDate, bool expiryEstimated, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        HouseholdId = householdId;
        IngredientId = ingredientId;
        Quantity = quantity;
        EnteredUnit = enteredUnit;
        Category = category;
        ExpiryDate = expiryDate;
        ExpiryEstimated = expiryEstimated;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid IngredientId { get; private set; }

    // Current amount in base units (g, ml, db); never negative.
    public decimal Quantity { get; private set; }

    // The unit the user entered; it only affects the display.
    public Unit EnteredUnit { get; private set; }

    public Category Category { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    public bool ExpiryEstimated { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // ADR-0008: optimistic concurrency token, mapped to PostgreSQL's xmin by the Infrastructure.
    public uint Version { get; private set; }

    public static (PantryItem Item, StockMovement Movement) Create(Guid householdId, Ingredient ingredient, decimal amount, Unit unit,
        Category category, DateOnly? expiryDate, DateOnly today, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        EnsureDimension(unit, ingredient.Dimension);
        var (expiry, estimated) = ResolveExpiry(category, expiryDate, today);

        var item = new PantryItem(Guid.CreateVersion7(), householdId, ingredient.Id, ToStoredQuantity(amount, unit), unit, category,
            expiry, estimated, now, now);
        return (item, StockMovement.Record(item, item.Quantity, MovementReason.Added, now));
    }

    // Returns null when the amount did not change. A decrease needs a reason (US-1); an increase is a
    // correction, because a new purchase is a new pantry item.
    public StockMovement? ChangeQuantity(decimal amount, Unit unit, MovementReason? reason, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        EnsureDimension(unit, Units.DimensionOf(EnteredUnit));
        var quantity = ToStoredQuantity(amount, unit);
        var delta = quantity - Quantity;
        MovementReason? movementReason = delta switch
        {
            0m => null,
            > 0m when reason is null or MovementReason.Corrected => MovementReason.Corrected,
            < 0m when reason is MovementReason.Consumed or MovementReason.Discarded or MovementReason.Corrected => reason,
            _ => throw new ArgumentException("A decrease needs a reason; an increase can only be a correction.", nameof(reason)),
        };

        EnteredUnit = unit;
        UpdatedAt = now;
        if (movementReason is not { } recorded)
        {
            return null;
        }

        Quantity = quantity;
        return StockMovement.Record(this, delta, recorded, now);
    }

    // A missing expiry date is estimated from the entry day (the Budapest date of CreatedAt), not from
    // the day of the edit: data_model.md, "a bevitel napja + a kategória napértéke".
    public void UpdateDetails(Category category, DateOnly? expiryDate, DateOnly entryDay, DateTimeOffset now)
    {
        (ExpiryDate, ExpiryEstimated) = ResolveExpiry(category, expiryDate, entryDay);
        Category = category;
        UpdatedAt = now;
    }

    private static decimal ToStoredQuantity(decimal amount, Unit unit)
    {
        var quantity = Units.ToBase(amount, unit);
        if (quantity > MaxQuantity || decimal.Round(quantity, 3) != quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "At most 3 decimals and 999 999 999.999 in base units.");
        }

        return quantity;
    }

    private static void EnsureDimension(Unit unit, Dimension dimension)
    {
        if (Units.DimensionOf(unit) != dimension)
        {
            throw new ArgumentException($"The unit {unit} does not measure {dimension} (ADR-0002).", nameof(unit));
        }
    }

    private static (DateOnly ExpiryDate, bool Estimated) ResolveExpiry(Category category, DateOnly? expiryDate, DateOnly today) =>
        expiryDate is { } given ? (given, false)
        : Categories.EstimateExpiry(category, today) is { } estimate ? (estimate, true)
        : throw new ArgumentException("Without a shelf-life estimate the expiry date is required.", nameof(expiryDate));
}
