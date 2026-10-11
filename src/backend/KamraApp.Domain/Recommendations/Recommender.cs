using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Recommendations;

// A recipe as the recommendation sees it. Quantities are in base units for the default servings.
public sealed record RecipeCandidate(Guid RecipeId, string Name, int Servings, IReadOnlyList<RecipeNeed> Needs)
{
    // CONTEXT.md, Alaphozzávaló: staples are never matched, deducted or listed as a shortage.
    public IEnumerable<RecipeNeed> NonStapleNeeds => Needs.Where(n => !n.IsStaple);
}

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

    // Names are ordered the Hungarian way ("Ágyas" before "Bableves"); the id settles exact ties.

    public static Recommendations Recommend(IEnumerable<RecipeCandidate> recipes, IEnumerable<StockItem> stock, DateOnly today)
    {
        var stockByIngredient = stock.ToLookup(s => s.IngredientId);
        var evaluated = recipes.Select(r => Evaluate(r, stockByIngredient, today)).ToList();

        // US-3: soon-expiring items used (desc), earliest expiry, name.
        var makeable = evaluated.Where(e => e.Recommendation.Shortages.Count == 0)
            .OrderByDescending(e => e.SoonExpiringItems)
            .ThenBy(e => e.EarliestExpiry ?? DateOnly.MaxValue)
            .ThenBy(e => e.Recommendation.Name, Collation.Hungarian)
            .ThenBy(e => e.Recommendation.RecipeId);

        // D1: fewer shortages, soon-expiring items used (desc), name.
        var almostMakeable = evaluated
            .Where(e => e.Recommendation.Shortages.Count is > 0 and <= MaxShortagesForAlmostMakeable)
            .OrderBy(e => e.Recommendation.Shortages.Count)
            .ThenByDescending(e => e.SoonExpiringItems)
            .ThenBy(e => e.Recommendation.Name, Collation.Hungarian)
            .ThenBy(e => e.Recommendation.RecipeId);

        return new Recommendations([.. makeable.Select(e => e.Recommendation)], [.. almostMakeable.Select(e => e.Recommendation)]);
    }

    private static Evaluated Evaluate(RecipeCandidate recipe, ILookup<Guid, StockItem> stock, DateOnly today)
    {
        var used = new List<(string Ingredient, StockItem Item)>();
        var shortages = new List<Shortage>();
        foreach (var need in recipe.NonStapleNeeds)
        {
            var items = stock[need.IngredientId].ToList();
            used.AddRange(Fefo.Allocate(items, need.Quantity).Select(a => (need.IngredientName, a.Item)));

            var available = items.Sum(i => i.Quantity);
            if (available < need.Quantity)
            {
                shortages.Add(new Shortage(need.IngredientName, Units.FromBase(need.Quantity - available, need.Unit),
                    Units.FromBase(available, need.Unit), Units.FromBase(need.Quantity, need.Unit), need.Unit));
            }
        }

        var soon = used.Where(u => Expiry.IsSoonExpiring(u.Item.ExpiryDate, today)).ToList();
        var recommendation = new Recommendation(recipe.RecipeId, recipe.Name, recipe.Servings,
            NamesByExpiry(soon), NamesByExpiry(used.Where(u => Expiry.IsExpired(u.Item.ExpiryDate, today))),
            [.. shortages.OrderBy(s => s.IngredientName, Collation.Hungarian)]);
        // Rank key 2 looks only at items that have not expired: an expired item is stock, but it never moves a
        // recipe up the list (Q5, E1).
        var notExpired = used.Where(u => !Expiry.IsExpired(u.Item.ExpiryDate, today)).ToList();
        return new Evaluated(recommendation, soon.Count, notExpired.Count == 0 ? null : notExpired.Min(u => u.Item.ExpiryDate));
    }

    // "2 hamarosan lejáró hozzávalót használ fel: paradicsom, paprika" - the earliest expiring first.
    private static List<string> NamesByExpiry(IEnumerable<(string Ingredient, StockItem Item)> used) =>
        [.. used.GroupBy(u => u.Ingredient)
            .OrderBy(g => g.Min(u => u.Item.ExpiryDate))
            .ThenBy(g => g.Key, Collation.Hungarian)
            .Select(g => g.Key)];

    private sealed record Evaluated(Recommendation Recommendation, int SoonExpiringItems, DateOnly? EarliestExpiry);
}
