using KamraApp.Application.Recommendations;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Infrastructure.Persistence;

public sealed class RecipeRepository(KamraDbContext db) : IRecipeRepository
{
    public Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {db.GetHashCode()}");
}
