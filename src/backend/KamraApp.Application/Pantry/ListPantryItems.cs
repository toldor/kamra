using KamraApp.Application.Common;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Pantry;

// US-1: the pantry list with search, category and "soon expiring" filters.
public sealed class ListPantryItems(IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<IReadOnlyList<PantryItemResponse>> ExecuteAsync(ListPantryItemsQuery query, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(query);
        var today = Today.Of(time);
        var filter = new PantryFilter(
            string.IsNullOrWhiteSpace(valid.Search) ? null : Ingredient.Normalize(valid.Search),
            valid.Category is null ? null : PantryInput.Parse<Category>(valid.Category),
            valid.ExpiringSoon == true ? today : null);
        var entries = await pantry.ListAsync(household.HouseholdId, filter, cancellationToken);

        return [.. entries.Select(e => PantryItemResponse.From(e.Item, e.IngredientName, today))];
    }
}
