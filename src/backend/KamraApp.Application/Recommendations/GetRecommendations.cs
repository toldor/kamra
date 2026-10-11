using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Application.Recommendations;

// PantryIsEmpty lets the page show the ux_flows empty-pantry text instead of "nothing matches".
public sealed record RecommendationsResponse(bool PantryIsEmpty, IReadOnlyList<Recommendation> Makeable, IReadOnlyList<Recommendation> AlmostMakeable);

// US-3: "Mit főzzek?" from the household's current stock.
public sealed class GetRecommendations(IRecipeRepository recipes, IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public Task<RecommendationsResponse> ExecuteAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {recipes.GetHashCode() + pantry.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}
