using KamraApp.Application.Common;
using KamraApp.Application.Cookings;
using KamraApp.Application.Recommendations;

namespace KamraApp.Application.Recipes;

// N4 (M10): archive a recipe the household can see; another household's own recipe is a 404 (QA-1).
// Archiving again changes nothing, so the first time stays.
public sealed class ArchiveRecipe(IRecipeRepository recipes, IRecipeCatalog catalog, ICurrentHousehold household, TimeProvider time)
{
    public async Task ExecuteAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        var householdId = household.HouseholdId;
        var details = await recipes.FindAsync(householdId, recipeId, cancellationToken) ?? throw CookingErrors.RecipeNotFound();
        if (!details.Archived)
        {
            await catalog.ArchiveAsync(householdId, recipeId, time.GetUtcNow(), cancellationToken);
        }
    }
}
