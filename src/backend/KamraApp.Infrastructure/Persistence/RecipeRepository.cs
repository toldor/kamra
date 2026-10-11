using KamraApp.Application.Recommendations;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// The system recipes and the household's own ones (from okt. 23), not retired (QA-1: household filter).
public sealed class RecipeRepository(KamraDbContext db) : IRecipeRepository
{
    public async Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var recipes = await db.Recipes.AsNoTracking()
            .Where(r => (r.HouseholdId == null || r.HouseholdId == householdId) && r.RetiredAt == null)
            .Select(r => new { r.Id, r.Name, r.Servings })
            .ToListAsync(cancellationToken);
        var recipeIds = recipes.Select(r => r.Id).ToList();
        var lines = await (from line in db.RecipeIngredients
                           join ingredient in db.Ingredients on line.IngredientId equals ingredient.Id
                           where recipeIds.Contains(line.RecipeId)
                                 && (ingredient.HouseholdId == null || ingredient.HouseholdId == householdId)
                           select new { line.RecipeId, line.IngredientId, line.Quantity, line.EnteredUnit, ingredient.Name, ingredient.SeedKey })
            .ToListAsync(cancellationToken);
        var linesByRecipe = lines.ToLookup(l => l.RecipeId);

        return [.. recipes.Select(r => new RecipeCandidate(r.Id, r.Name, r.Servings,
            [.. linesByRecipe[r.Id].Select(l => new RecipeNeed(l.IngredientId, l.Name, l.Quantity, l.EnteredUnit, Ingredient.IsStapleKey(l.SeedKey)))]))];
    }

    public Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken) =>
        throw new NotImplementedException("red phase");
}
