using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;

namespace KamraApp.Application.Recipes;

// N3 (M9): an own recipe from the household's visible ingredients; another household's ingredient is
// reported exactly like a missing one (QA-1, P17).
public sealed class CreateRecipe(IIngredientRepository ingredients, IRecipeCatalog catalog, ICurrentHousehold household, TimeProvider time)
{
    public Task<RecipeResponse> ExecuteAsync(CreateRecipeRequest? request, CancellationToken cancellationToken)
    {
        _ = (ingredients, catalog, household, time);
        throw new NotImplementedException();
    }
}
