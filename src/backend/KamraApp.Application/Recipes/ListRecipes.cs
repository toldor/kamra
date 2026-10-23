using KamraApp.Application.Common;

namespace KamraApp.Application.Recipes;

// N2: the Receptek page, in Hungarian alphabetical order (V-33), the ingredients of each recipe too.
public sealed class ListRecipes(IRecipeCatalog catalog, ICurrentHousehold household)
{
    public Task<IReadOnlyList<RecipeResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = (catalog, household);
        throw new NotImplementedException();
    }
}
