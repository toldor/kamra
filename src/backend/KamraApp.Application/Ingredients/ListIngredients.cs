using KamraApp.Application.Common;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Ingredients;

// D4: Minimum is the household's minimum for the ingredient (CONTEXT.md, Minimumszint), or null.
public sealed record IngredientResponse(Guid Id, string Name, Dimension Dimension, Category DefaultCategory, AmountResponse? Minimum);

public sealed class ListIngredients(IIngredientRepository ingredients, IShoppingListRepository shopping, ICurrentHousehold household)
{
    public async Task<IReadOnlyList<IngredientResponse>> ExecuteAsync(ListIngredientsQuery query, CancellationToken cancellationToken)
    {
        var search = RequestValidator.Validate(query).Search;
        var normalized = string.IsNullOrWhiteSpace(search) ? null : Ingredient.Normalize(search);
        var visible = await ingredients.ListVisibleAsync(household.HouseholdId, normalized, cancellationToken);
        var minimums = (await shopping.ListMinimumsAsync(household.HouseholdId, cancellationToken)).ToDictionary(m => m.IngredientId);
        // V-33: sorted here, because the database's en_US collation is not the Hungarian alphabet.
        return [.. visible.OrderBy(i => i.Name, Collation.Hungarian).Select(i => new IngredientResponse(i.Id, i.Name, i.Dimension, i.DefaultCategory,
            minimums.TryGetValue(i.Id, out var minimum) ? AmountResponse.Of(minimum.MinimumQuantity, minimum.EnteredUnit) : null))];
    }
}
