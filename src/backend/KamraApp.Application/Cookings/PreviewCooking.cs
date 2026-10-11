using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Domain;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Cookings;

// US-4: the confirmation screen for a recipe and a number of servings (default: the recipe's).
public sealed class PreviewCooking(IRecipeRepository recipes, IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<CookingPreviewResponse> ExecuteAsync(Guid recipeId, CookingPreviewQuery query, CancellationToken cancellationToken)
    {
        var servings = RequestValidator.Validate(query).Servings;
        var details = await recipes.FindAsync(household.HouseholdId, recipeId, cancellationToken) ?? throw CookingErrors.RecipeNotFound();
        var recipe = details.Candidate;
        var count = servings ?? recipe.Servings;
        var stock = (await pantry.ListAsync(household.HouseholdId, PantryFilter.None, cancellationToken)).Select(e => StockItem.From(e.Item));
        var today = Today.Of(time);

        var lines = CookingCalculator.Preview(recipe, count, stock)
            .OrderBy(l => l.Need.IngredientName, Collation.Hungarian)
            .Select(l =>
            {
                decimal InRecipeUnit(decimal quantity) => Units.FromBase(quantity, l.Need.Unit);
                return new CookingPreviewLine(l.Need.IngredientId, l.Need.IngredientName, l.Need.Unit, InRecipeUnit(l.Required),
                    InRecipeUnit(l.Available), InRecipeUnit(l.DefaultUsed), InRecipeUnit(l.Shortage),
                    [.. l.Allocations.Select(a => new CookingAllocation(a.Item.ExpiryDate, InRecipeUnit(a.Amount), Expiry.IsExpired(a.Item.ExpiryDate, today)))]);
            });
        var staples = recipe.Needs.Where(n => n.IsStaple).Select(n => n.IngredientName).Order(Collation.Hungarian);

        return new CookingPreviewResponse(recipe.RecipeId, recipe.Name, count, recipe.Servings,
            details.Instructions, [.. staples], [.. lines]);
    }
}
