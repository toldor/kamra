using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Pantry;

// CONTEXT.md, Készlettétel. The quantity changes only together with a stock movement, so the
// quantity always equals the sum of the item's movements (data_model.md).
public sealed class PantryItem
{
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
        Category category, DateOnly? expiryDate, DateOnly today, DateTimeOffset now) => throw new NotImplementedException();

    // Returns null when the amount did not change. A decrease needs a reason (US-1); an increase is a
    // correction, because a new purchase is a new pantry item.
    public StockMovement? ChangeQuantity(decimal amount, Unit unit, MovementReason? reason, DateTimeOffset now) =>
        throw new NotImplementedException();

    // A missing expiry date is estimated from the category, as on creation.
    public void UpdateDetails(Category category, DateOnly? expiryDate, DateOnly today, DateTimeOffset now) =>
        throw new NotImplementedException();
}
