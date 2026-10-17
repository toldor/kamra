using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Ingredients;

// CONTEXT.md, Minimumszint: optional, per household and ingredient; below it a Consumed decrease suggests
// buying more (US-5, data_model.md IngredientMinimums).
public sealed class IngredientMinimum
{
    private IngredientMinimum(Guid householdId, Guid ingredientId, decimal minimumQuantity, Unit enteredUnit)
    {
        HouseholdId = householdId;
        IngredientId = ingredientId;
        MinimumQuantity = minimumQuantity;
        EnteredUnit = enteredUnit;
    }

    public Guid HouseholdId { get; private set; }

    public Guid IngredientId { get; private set; }

    // Base units, > 0.
    public decimal MinimumQuantity { get; private set; }

    public Unit EnteredUnit { get; private set; }

    public static IngredientMinimum Create(Guid householdId, Ingredient ingredient, decimal amount, Unit unit) =>
        throw new NotImplementedException();

    public void Change(decimal amount, Unit unit) => throw new NotImplementedException();
}
