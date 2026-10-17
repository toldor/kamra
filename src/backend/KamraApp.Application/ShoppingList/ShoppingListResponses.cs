using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// An amount in the unit it is shown in. Optional amounts are a nullable AmountResponse, never a nullable Unit:
// the OpenAPI generator would add null to the shared Unit schema, and every unit field would become nullable.
public sealed record AmountResponse(decimal Value, Unit Unit);

// Amount: null for a row without an amount.
public sealed record ShoppingListItemResponse(Guid Id, Guid IngredientId, string IngredientName, AmountResponse? Amount, bool Checked);

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
