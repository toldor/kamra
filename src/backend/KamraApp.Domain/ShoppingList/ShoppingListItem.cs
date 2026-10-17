using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.ShoppingList;

// CONTEXT.md, Bevásárlólista-tétel: one row per ingredient, the amount optional (data_model.md).
public sealed class ShoppingListItem
{
    // M7: the merged amount, in the row's unit, stays within the input limit.
    public const decimal MaxAmount = 100_000m;

    private ShoppingListItem(Guid id, Guid householdId, Guid ingredientId, decimal? quantity, Unit? enteredUnit, DateTimeOffset addedAt)
    {
        Id = id;
        HouseholdId = householdId;
        IngredientId = ingredientId;
        Quantity = quantity;
        EnteredUnit = enteredUnit;
        AddedAt = addedAt;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid IngredientId { get; private set; }

    // Base units; null when no source gave an amount.
    public decimal? Quantity { get; private set; }

    public Unit? EnteredUnit { get; private set; }

    // Bought (M2).
    public bool Checked { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    // The amount in the row's unit, for display.
    public decimal? Amount => throw new NotImplementedException();

    // The caller has checked the unit against the ingredient's dimension (ADR-0002).
    public static ShoppingListItem Create(Guid householdId, Guid ingredientId, decimal? amount, Unit? unit, DateTimeOffset now) =>
        throw new NotImplementedException();

    // M2, D3, V-22: an unchecked row adds up (null keeps the known amount, different units meet in the smaller
    // one); a checked row starts again with the new amount, unchecked. False, and nothing changes, when the
    // result would exceed MaxAmount (M7).
    public bool TryAdd(decimal? amount, Unit? unit) => throw new NotImplementedException();

    public void Update(bool isChecked, decimal? amount, Unit? unit) => throw new NotImplementedException();
}
