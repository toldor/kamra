using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Cookings;

// The confirmation screen. Amounts are in the recipe's unit; DefaultUsed is the smaller of required
// and available, Allocations its FEFO split ("melyik készlettételből vonódik le").
public sealed record CookingPreviewResponse(
    Guid RecipeId,
    string Name,
    int Servings,
    int RecipeServings,
    string Instructions,
    IReadOnlyList<string> Staples,
    IReadOnlyList<CookingPreviewLine> Lines);

public sealed record CookingPreviewLine(
    Guid IngredientId,
    string IngredientName,
    Unit Unit,
    decimal Required,
    decimal Available,
    decimal DefaultUsed,
    decimal Shortage,
    IReadOnlyList<CookingAllocation> Allocations);

public sealed record CookingAllocation(DateOnly ExpiryDate, decimal Amount, bool Expired);

// Built from the stored cooking, so a repeated request gets the same body (ADR-0008). SuggestedIngredients: the
// depleted ones that became shopping suggestions; one already on the list unchecked is not (scope_contract.md US-4).
public sealed record CookingResponse(
    Guid CookingId,
    Guid RecipeId,
    string RecipeName,
    int Servings,
    DateTimeOffset CookedAt,
    IReadOnlyList<CookingResponseLine> Lines,
    IReadOnlyList<string> DepletedIngredients,
    IReadOnlyList<string> SuggestedIngredients);

public sealed record CookingResponseLine(string IngredientName, Unit Unit, decimal Required, decimal Used, decimal Shortage);

// Created: 201 for the first request, 200 for a repeated one.
public sealed record ConfirmCookingResult(CookingResponse Cooking, bool Created);
