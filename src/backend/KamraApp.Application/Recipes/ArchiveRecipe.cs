using KamraApp.Application.Common;
using KamraApp.Application.Recommendations;

namespace KamraApp.Application.Recipes;

// N4 (M10): archive a recipe the household can see; another household's own recipe is a 404 (QA-1).
public sealed class ArchiveRecipe(IRecipeRepository recipes, IRecipeCatalog catalog, ICurrentHousehold household, TimeProvider time)
{
    public Task ExecuteAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        _ = (recipes, catalog, household, time);
        throw new NotImplementedException();
    }
}
