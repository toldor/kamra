using KamraApp.Application.Common;
using KamraApp.Domain;

namespace KamraApp.Application.Recipes;

// N2: the Receptek page, in Hungarian alphabetical order (V-33), the ingredients of each recipe too.
public sealed class ListRecipes(IRecipeCatalog catalog, ICurrentHousehold household)
{
    public async Task<IReadOnlyList<RecipeResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        [.. (await catalog.ListAsync(household.HouseholdId, cancellationToken))
            .OrderBy(r => r.Name, Collation.Hungarian)
            .Select(r => r with { Ingredients = [.. r.Ingredients.Order(Collation.Hungarian)] })];
}
