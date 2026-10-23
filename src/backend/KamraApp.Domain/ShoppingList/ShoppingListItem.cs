using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.ShoppingList;

// CONTEXT.md, Bevásárlólista-tétel: one row per ingredient, the amount optional (data_model.md).
public sealed class ShoppingListItem
{
    // The merged amount, in the row's unit, stays within the input limit (data_model.md).
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

    // Bought: a new amount starts the row again (data_model.md).
    public bool Checked { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    // The amount in the row's unit, for display.
    public decimal? Amount => Quantity is { } quantity && EnteredUnit is { } unit ? Units.FromBase(quantity, unit) : null;

    // The caller has checked the unit against the ingredient's dimension (ADR-0002).
    public static ShoppingListItem Create(Guid householdId, Guid ingredientId, decimal? amount, Unit? unit, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), householdId, ingredientId, ToBase(amount, unit), unit, now);

    // data_model.md, V-22: an unchecked row adds up (null keeps the known amount, different units meet in the smaller
    // one); a checked row starts again with the new amount, unchecked. False, and nothing changes, when the
    // result would exceed MaxAmount.
    public bool TryAdd(decimal? amount, Unit? unit)
    {
        var added = ToBase(amount, unit);
        if (EnteredUnit is { } current && unit is { } other)
        {
            Units.EnsureDimension(other, Units.DimensionOf(current));
        }

        if (Checked || Quantity is null)
        {
            Set(added, unit);
            return true;
        }

        if (added is not { } more)
        {
            return true;
        }

        var smaller = Units.ToBase(1, unit!.Value) < Units.ToBase(1, EnteredUnit!.Value) ? unit.Value : EnteredUnit.Value;
        var total = Quantity.Value + more;
        if (Units.FromBase(total, smaller) > MaxAmount)
        {
            return false;
        }

        Set(total, smaller);
        return true;
    }

    // Without an amount only the check changes; the row keeps its amount (api.md).
    public void Update(bool isChecked, decimal? amount, Unit? unit)
    {
        if (amount is not null || unit is not null)
        {
            Quantity = ToBase(amount, unit);
            EnteredUnit = unit;
        }

        Checked = isChecked;
    }

    // The row starts again, unchecked, with this amount.
    private void Set(decimal? quantity, Unit? unit)
    {
        Checked = false;
        Quantity = quantity;
        EnteredUnit = unit;
    }

    private static decimal? ToBase(decimal? amount, Unit? unit)
    {
        if (amount is null != unit is null)
        {
            throw new ArgumentException("An amount and its unit come together, or neither.", nameof(amount));
        }

        if (amount is not { } value)
        {
            return null;
        }

        if (value <= 0 || value > MaxAmount || decimal.Round(value, 3) != value)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Between 0.001 and 100 000, with at most 3 decimals.");
        }

        return Units.ToBase(value, unit!.Value);
    }
}
