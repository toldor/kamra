using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;
using ValidationException = KamraApp.Application.Common.ValidationException;

namespace KamraApp.Application.Cookings;

// US-4: the confirmation screen for a recipe and a number of servings (default: the recipe's).
public sealed class PreviewCooking(IRecipeRepository recipes, IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<CookingPreviewResponse> ExecuteAsync(Guid recipeId, int? servings, CancellationToken cancellationToken)
    {
        if (servings is < 1 or > CookingInput.MaxServings)
        {
            throw ValidationException.ForField("servings", CookingInput.ServingsMessage);
        }

        var details = await recipes.FindAsync(household.HouseholdId, recipeId, cancellationToken) ?? throw CookingErrors.RecipeNotFound();
        var recipe = details.Candidate;
        var count = servings ?? recipe.Servings;
        var stock = (await pantry.ListAsync(household.HouseholdId, PantryFilter.None, cancellationToken)).Select(e => StockItem.From(e.Item));
        var today = Today.Of(time);

        var lines = CookingCalculator.Preview(recipe, count, stock)
            .OrderBy(l => l.Need.IngredientName, Recommender.HungarianOrder)
            .Select(l =>
            {
                decimal Show(decimal quantity) => Units.FromBase(quantity, l.Need.Unit);
                return new CookingPreviewLine(l.Need.IngredientId, l.Need.IngredientName, l.Need.Unit, Show(l.Required), Show(l.Available),
                    Show(l.DefaultUsed), Show(Math.Max(0, l.Required - l.Available)),
                    [.. l.Allocations.Select(a => new CookingAllocation(a.Item.ExpiryDate, Show(a.Amount), Expiry.IsExpired(a.Item.ExpiryDate, today)))]);
            });
        var staples = recipe.Needs.Where(n => n.IsStaple).Select(n => n.IngredientName).Order(Recommender.HungarianOrder);

        return new CookingPreviewResponse(recipe.RecipeId, recipe.Name, count, recipe.Servings,
            details.Instructions, [.. staples], [.. lines]);
    }
}
