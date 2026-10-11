using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;

namespace KamraApp.Application.Cookings;

// US-4: the confirmation screen for a recipe and a number of servings (default: the recipe's).
public sealed class PreviewCooking(IRecipeRepository recipes, IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public Task<CookingPreviewResponse> ExecuteAsync(Guid recipeId, int? servings, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {recipes.GetHashCode() + pantry.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}
