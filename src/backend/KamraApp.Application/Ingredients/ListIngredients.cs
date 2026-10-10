using KamraApp.Application.Common;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Ingredients;

public sealed record IngredientResponse(Guid Id, string Name, Dimension Dimension, Category DefaultCategory);

public sealed class ListIngredients(IIngredientRepository ingredients, ICurrentHousehold household)
{
    public async Task<IReadOnlyList<IngredientResponse>> ExecuteAsync(ListIngredientsQuery query, CancellationToken cancellationToken)
    {
        var search = RequestValidator.Validate(query).Search;
        var normalized = string.IsNullOrWhiteSpace(search) ? null : Ingredient.Normalize(search);
        var visible = await ingredients.ListVisibleAsync(household.HouseholdId, normalized, cancellationToken);
        return [.. visible.Select(i => new IngredientResponse(i.Id, i.Name, i.Dimension, i.DefaultCategory))];
    }
}
