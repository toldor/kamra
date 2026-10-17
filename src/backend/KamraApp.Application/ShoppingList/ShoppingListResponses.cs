using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// An amount in the unit it is shown in. Optional amounts are a nullable AmountResponse, never a nullable Unit:
// the OpenAPI generator would add null to the shared Unit schema, and every unit field would become nullable.
public sealed record AmountResponse(decimal Value, Unit Unit)
{
    public static AmountResponse? Of(decimal? quantity, Unit? unit) =>
        quantity is { } value && unit is { } shown ? new AmountResponse(Normalize(Units.FromBase(value, shown)), shown) : null;

    // Without trailing zeros: numeric(12,3) reads back 200 as 200.000, and a repeated request must get the same
    // body as the first one, built from memory (ADR-0008).
    private static decimal Normalize(decimal value) => value / 1.000000000000000000000000000000000m;
}

// Amount: null for a row without an amount.
public sealed record ShoppingListItemResponse(Guid Id, Guid IngredientId, string IngredientName, AmountResponse? Amount, bool Checked)
{
    public static ShoppingListItemResponse From(ShoppingListItem item, string ingredientName) =>
        new(item.Id, item.IngredientId, ingredientName, AmountResponse.Of(item.Quantity, item.EnteredUnit), item.Checked);
}

// M12, M13: why the suggestion was made and how much to buy. For BelowMinimum the amounts are in the
// minimum's unit: the current total, the minimum and the part missing up to it; for Depleted all three are
// null. The dimension lets the screen offer the units for an amount.
public sealed record ShoppingSuggestionResponse(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    Dimension Dimension,
    SuggestionTrigger Trigger,
    DateTimeOffset CreatedAt,
    AmountResponse? CurrentAmount,
    AmountResponse? MinimumAmount,
    AmountResponse? SuggestedAmount);
