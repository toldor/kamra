using KamraApp.Application.Recipes;
using KamraApp.Application.Recommendations;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Recipes;
using KamraApp.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// The system recipes and the household's own ones, not retired and not archived by the household (QA-1:
// household filter; M10).
public sealed class RecipeRepository(KamraDbContext db) : IRecipeRepository, IRecipeCatalog
{
    public async Task<IReadOnlyList<RecipeResponse>> ListAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var recipes = await Listed(householdId)
            .Select(r => new { r.Id, r.Name, r.Servings, Own = r.HouseholdId != null })
            .ToListAsync(cancellationToken);
        var needs = await NeedsAsync(householdId, [.. recipes.Select(r => r.Id)], cancellationToken);
        return [.. recipes.Select(r => new RecipeResponse(r.Id, r.Name, r.Servings, r.Own, [.. needs[r.Id].Select(n => n.IngredientName)]))];
    }

    public void Add(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients)
    {
        db.Recipes.Add(recipe);
        db.RecipeIngredients.AddRange(ingredients);
    }

    // N4: ON CONFLICT keeps the first row, also when two requests archive at the same time.
    public async Task ArchiveAsync(Guid householdId, Guid recipeId, DateTimeOffset archivedAt, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlAsync(
            $"""INSERT INTO "RecipeArchives" ("HouseholdId", "RecipeId", "ArchivedAt") VALUES ({householdId}, {recipeId}, {archivedAt}) ON CONFLICT DO NOTHING""",
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAndCommitAsync(cancellationToken);

    public async Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var recipes = await Listed(householdId)
            .Select(r => new { r.Id, r.Name, r.Servings })
            .ToListAsync(cancellationToken);
        var needs = await NeedsAsync(householdId, [.. recipes.Select(r => r.Id)], cancellationToken);
        return [.. recipes.Select(r => new RecipeCandidate(r.Id, r.Name, r.Servings, [.. needs[r.Id]]))];
    }

    // Also a retired or archived recipe: a repeated cooking request must still find what it cooked (D-c).
    public async Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.AsNoTracking()
            .Where(r => r.Id == recipeId && (r.HouseholdId == null || r.HouseholdId == householdId))
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Servings,
                r.Instructions,
                Archived = db.RecipeArchives.Any(a => a.HouseholdId == householdId && a.RecipeId == r.Id),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var needs = await NeedsAsync(householdId, [recipe.Id], cancellationToken);
        return new RecipeDetails(new RecipeCandidate(recipe.Id, recipe.Name, recipe.Servings, [.. needs[recipe.Id]]), recipe.Instructions,
            recipe.Archived);
    }

    // The recipes the household is offered: the Receptek page and the recommendations.
    private IQueryable<Recipe> Listed(Guid householdId) =>
        db.Recipes.AsNoTracking().Where(r => (r.HouseholdId == null || r.HouseholdId == householdId) && r.RetiredAt == null
            && !db.RecipeArchives.Any(a => a.HouseholdId == householdId && a.RecipeId == r.Id));

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
