using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Domain.Cookings;

// One line of the confirmation screen, in base units: what the servings need, what is in stock, the
// default used amount (the smaller of the two) and the FEFO split of that default.
public sealed record PreviewLine(RecipeNeed Need, decimal Required, decimal Available, decimal DefaultUsed,
    IReadOnlyList<(StockItem Item, decimal Amount)> Allocations)
{
    // CONTEXT.md, Hiány: what the stock lacks for the servings.
    public decimal Shortage => Math.Max(0, Required - Available);
}

// US-4: linear scaling to the servings; g and ml are rounded to whole numbers (a half rounds up, not to
// even), pieces always up.
public static class CookingCalculator
{
    public static decimal ScaleRequired(decimal baseQuantity, Unit unit, int recipeServings, int servings)
    {
        var scaled = baseQuantity * servings / recipeServings;
        return Units.DimensionOf(unit) == Dimension.Count
            ? Math.Ceiling(scaled)
            : Math.Round(scaled, 0, MidpointRounding.AwayFromZero);
    }

    // The recipe's non-staple ingredients in recipe order.
    public static IReadOnlyList<PreviewLine> Preview(RecipeCandidate recipe, int servings, IEnumerable<StockItem> stock)
    {
        var byIngredient = stock.ToLookup(s => s.IngredientId);
        return [.. recipe.NonStapleNeeds.Select(need =>
        {
            var required = ScaleRequired(need.Quantity, need.Unit, recipe.Servings, servings);
            var items = byIngredient[need.IngredientId].ToList();
            var available = items.Sum(i => i.Quantity);
            var defaultUsed = Math.Min(required, available);
            return new PreviewLine(need, required, available, defaultUsed, Fefo.Allocate(items, defaultUsed));
        })];
    }
}
