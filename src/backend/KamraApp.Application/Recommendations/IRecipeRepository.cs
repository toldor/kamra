using KamraApp.Domain.Recommendations;

namespace KamraApp.Application.Recommendations;

// The recipes a household can cook: the system recipes and its own ones, not retired, with their
// ingredients' names and staple flags.
public interface IRecipeRepository
{
    Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken);

    // One recipe the household can see, with its instructions; null when missing or another household's.
    Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken);
}

public sealed record RecipeDetails(RecipeCandidate Candidate, string Instructions);
