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
        var needs = await NeedsAsync(householdId, [.. recipes.Select(r => r.Id)], cancellationToken);
        return [.. recipes.Select(r => new RecipeCandidate(r.Id, r.Name, r.Servings, [.. needs[r.Id]]))];
    }

    // Also a retired recipe: a repeated cooking request must still find what it cooked.
    public async Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.AsNoTracking()
            .Where(r => r.Id == recipeId && (r.HouseholdId == null || r.HouseholdId == householdId))
            .Select(r => new { r.Id, r.Name, r.Servings, r.Instructions })
            .SingleOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var needs = await NeedsAsync(householdId, [recipe.Id], cancellationToken);
        return new RecipeDetails(new RecipeCandidate(recipe.Id, recipe.Name, recipe.Servings, [.. needs[recipe.Id]]), recipe.Instructions);
    }

    private async Task<ILookup<Guid, RecipeNeed>> NeedsAsync(Guid householdId, List<Guid> recipeIds, CancellationToken cancellationToken)
    {
        var lines = await (from line in db.RecipeIngredients
                           join ingredient in db.Ingredients on line.IngredientId equals ingredient.Id
                           where recipeIds.Contains(line.RecipeId)
                                 && (ingredient.HouseholdId == null || ingredient.HouseholdId == householdId)
                           select new { line.RecipeId, line.IngredientId, line.Quantity, line.EnteredUnit, ingredient.Name, ingredient.SeedKey })
            .ToListAsync(cancellationToken);
        return lines.ToLookup(l => l.RecipeId,
            l => new RecipeNeed(l.IngredientId, l.Name, l.Quantity, l.EnteredUnit, Ingredient.IsStapleKey(l.SeedKey)));
    }
}
