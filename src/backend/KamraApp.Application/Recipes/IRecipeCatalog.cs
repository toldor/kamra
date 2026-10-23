using KamraApp.Domain.Recipes;

namespace KamraApp.Application.Recipes;

// US-3, the Receptek page: the household's recipes (system and own, without retired and archived ones),
// saving an own recipe, and archiving (M10). Every read filters on the household (QA-1). A second port of the
// recipe aggregate next to IRecipeRepository (the reads of recommending and cooking), so their callers and test
// fakes stay as they are (T3 review S9); the same class implements both.
public interface IRecipeCatalog
{
    Task<IReadOnlyList<RecipeResponse>> ListAsync(Guid householdId, CancellationToken cancellationToken);

    void Add(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients);

    // Idempotent: a second archiving keeps the first row and its time (N4), concurrent ones too.
    Task ArchiveAsync(Guid householdId, Guid recipeId, DateTimeOffset archivedAt, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

// Ingredients: every ingredient's name, staples too (N2).
public sealed record RecipeResponse(Guid Id, string Name, int Servings, bool Own, IReadOnlyList<string> Ingredients);
