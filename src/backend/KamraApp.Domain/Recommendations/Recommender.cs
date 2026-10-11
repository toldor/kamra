using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Recommendations;

// A recipe as the recommendation sees it. Quantities are in base units for the default servings.
public sealed record RecipeCandidate(Guid RecipeId, string Name, int Servings, IReadOnlyList<RecipeNeed> Needs);

public sealed record RecipeNeed(Guid IngredientId, string IngredientName, decimal Quantity, Unit Unit, bool IsStaple);

// Amounts in the recipe's unit, e.g. "tejföl: 50 g hiányzik (150 g van, 200 g kell)".
public sealed record Shortage(string IngredientName, decimal Missing, decimal Available, decimal Required, Unit Unit);

public sealed record Recommendation(
    Guid RecipeId,
    string Name,
    int Servings,
    IReadOnlyList<string> SoonExpiringIngredients,
    IReadOnlyList<string> ExpiredIngredients,
    IReadOnlyList<Shortage> Shortages);

public sealed record Recommendations(IReadOnlyList<Recommendation> Makeable, IReadOnlyList<Recommendation> AlmostMakeable);

// US-3 (CONTEXT.md, Ajánlás): a deterministic list from the current stock. Staples are ignored; a recipe
// with no shortage is makeable, with 1-2 almost makeable. "Used" items are the FEFO split of the default
// servings (D2), so the recommendation and the cooking speak about the same items.
public static class Recommender
{
    public const int MaxShortagesForAlmostMakeable = 2;

    public static Recommendations Recommend(IEnumerable<RecipeCandidate> recipes, IEnumerable<StockItem> stock, DateOnly today) =>
        throw new NotImplementedException();
}
