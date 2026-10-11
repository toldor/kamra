using KamraApp.Application.Common;
using KamraApp.Domain;
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

        // Earliest expiry first, then the Hungarian alphabet (V-33: not the database's en_US collation).
        return [.. entries.OrderBy(e => e.Item.ExpiryDate).ThenBy(e => e.IngredientName, Collation.Hungarian).ThenBy(e => e.Item.Id)
            .Select(e => PantryItemResponse.From(e.Item, e.IngredientName, today))];
    }
}
