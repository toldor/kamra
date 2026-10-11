using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Domain.Cookings;

// One line of the confirmation screen, in base units: what the servings need, what is in stock, the
// default used amount (the smaller of the two) and the FEFO split of that default.
public sealed record PreviewLine(RecipeNeed Need, decimal Required, decimal Available, decimal DefaultUsed,
    IReadOnlyList<(StockItem Item, decimal Amount)> Allocations);

// US-4: linear scaling to the servings; g and ml are rounded to whole numbers (a half rounds up, not to
// even), pieces always up.
public static class CookingCalculator
{
    public const int MaxServings = 100;

    public static decimal ScaleRequired(decimal baseQuantity, Unit unit, int recipeServings, int servings) =>
        throw new NotImplementedException();

    public static IReadOnlyList<PreviewLine> Preview(RecipeCandidate recipe, int servings, IEnumerable<StockItem> stock) =>
        throw new NotImplementedException();
}
