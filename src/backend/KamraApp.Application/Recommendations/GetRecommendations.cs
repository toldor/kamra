using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Application.Recommendations;

// PantryIsEmpty lets the page show the ux_flows empty-pantry text instead of "nothing matches".
public sealed record RecommendationsResponse(
    bool PantryIsEmpty,
    IReadOnlyList<RecommendationResponse> Makeable,
    IReadOnlyList<RecommendationResponse> AlmostMakeable);

public sealed record RecommendationResponse(
    Guid RecipeId,
    string Name,
    int Servings,
    IReadOnlyList<string> SoonExpiringIngredients,
    IReadOnlyList<string> ExpiredIngredients,
    IReadOnlyList<ShortageResponse> Shortages)
{
    public static RecommendationResponse From(Recommendation recommendation) =>
        new(recommendation.RecipeId, recommendation.Name, recommendation.Servings, recommendation.SoonExpiringIngredients,
            recommendation.ExpiredIngredients, [.. recommendation.Shortages.Select(ShortageResponse.From)]);
}

// In the recipe's unit: "tejföl: 50 g hiányzik (150 g van, 200 g kell)".
public sealed record ShortageResponse(string IngredientName, decimal Missing, decimal Available, decimal Required, Unit Unit)
{
    public static ShortageResponse From(Shortage shortage) =>
        new(shortage.IngredientName, shortage.Missing, shortage.Available, shortage.Required, shortage.Unit);
}

// US-3: "Mit főzzek?" from the household's current stock.
public sealed class GetRecommendations(IRecipeRepository recipes, IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<RecommendationsResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var stock = await pantry.ListAsync(household.HouseholdId, PantryFilter.None, cancellationToken);
        // ux_flows (E2): from an empty pantry there is nothing to recommend, not even a recipe of 1-2 ingredients.
        if (stock.Count == 0)
        {
            return new RecommendationsResponse(PantryIsEmpty: true, [], []);
        }

        var candidates = await recipes.ListCandidatesAsync(household.HouseholdId, cancellationToken);
        var result = Recommender.Recommend(candidates, stock.Select(e => StockItem.From(e.Item)), Today.Of(time));
        return new RecommendationsResponse(PantryIsEmpty: false,
            [.. result.Makeable.Select(RecommendationResponse.From)], [.. result.AlmostMakeable.Select(RecommendationResponse.From)]);
    }
}
