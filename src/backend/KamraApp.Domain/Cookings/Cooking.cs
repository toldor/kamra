using KamraApp.Domain.Pantry;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Domain.Cookings;

// CONTEXT.md, Főzés: a recipe cooked for a number of servings. Each line keeps what the recipe needed
// (scaled) and what was really used; only the used amount leaves the pantry (US-4).
public sealed class Cooking
{
    // S5-D5: keeps the scaled amounts inside numeric(12,3), like PantryItem's quantity limit.
    public const int MaxServings = 100;

    private readonly List<CookingLine> _lines = [];

    private Cooking(Guid id, Guid householdId, Guid recipeId, int servings, Guid requestId, DateTimeOffset cookedAt)
    {
        Id = id;
        HouseholdId = householdId;
        RecipeId = recipeId;
        Servings = servings;
        RequestId = requestId;
        CookedAt = cookedAt;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid RecipeId { get; private set; }

    public int Servings { get; private set; }

    // ADR-0008: the confirmation screen's request id; unique per household, so a repeated request finds it.
    public Guid RequestId { get; private set; }

    public DateTimeOffset CookedAt { get; private set; }

    public IReadOnlyList<CookingLine> Lines => _lines;

    // Deducts the used amounts FEFO from the given pantry items (expired ones first, Q5). The caller has
    // already checked that the stock covers them; staples never become lines.
    public static (Cooking Cooking, IReadOnlyList<StockMovement> Movements) Create(Guid householdId, RecipeCandidate recipe,
        int servings, Guid requestId, IReadOnlyDictionary<Guid, decimal> used, IReadOnlyList<PantryItem> items, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(servings);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(servings, MaxServings);
        var needs = recipe.NonStapleNeeds.ToList();
        if (needs.Count != used.Count || needs.Any(n => !used.ContainsKey(n.IngredientId)))
        {
            throw new ArgumentException("A used amount is needed for every non-staple ingredient, and only for those.", nameof(used));
        }

        var stock = items.Where(i => i.HouseholdId == householdId).ToLookup(i => i.IngredientId);
        // P10: every line is checked before anything is deducted.
        if (needs.Any(n => used[n.IngredientId] < 0 || used[n.IngredientId] > stock[n.IngredientId].Sum(i => i.Quantity)))
        {
            throw new InvalidOperationException("The stock does not cover the used amounts.");
        }

        var cooking = new Cooking(Guid.CreateVersion7(), householdId, recipe.RecipeId, servings, requestId, now);
        var movements = new List<StockMovement>();
        foreach (var need in needs)
        {
            var amount = used[need.IngredientId];
            var ingredientItems = stock[need.IngredientId].ToList();
            foreach (var (item, taken) in Fefo.Allocate(ingredientItems.Select(StockItem.From), amount).Where(a => a.Amount > 0))
            {
                movements.Add(ingredientItems.Single(i => i.Id == item.PantryItemId).Consume(taken, cooking.Id, now));
            }

            var depleted = amount > 0 && ingredientItems.Sum(i => i.Quantity) == 0;
            cooking._lines.Add(CookingLine.Create(cooking.Id, need.IngredientId,
                CookingCalculator.ScaleRequired(need.Quantity, need.Unit, recipe.Servings, servings), amount, depleted));
        }

        return (cooking, movements);
    }

    // M5: the cooking created a shopping suggestion for this ingredient; stored, so a repeated request gets the
    // same answer (ADR-0008).
    public void MarkSuggested(Guid ingredientId) => throw new NotImplementedException();

    // ADR-0008: a repeated request is the same cooking only with the same recipe, servings and every used amount.
    public bool Matches(Guid recipeId, int servings, IReadOnlyDictionary<Guid, decimal> used) =>
        RecipeId == recipeId && Servings == servings && used.Count == _lines.Count
        && _lines.All(l => used.TryGetValue(l.IngredientId, out var amount) && amount == l.UsedQuantity);
}

public sealed class CookingLine
{
    private CookingLine(Guid cookingId, Guid ingredientId, decimal requiredQuantity, decimal usedQuantity, bool depleted)
    {
        Depleted = depleted;
        CookingId = cookingId;
        IngredientId = ingredientId;
        RequiredQuantity = requiredQuantity;
        UsedQuantity = usedQuantity;
    }

    public Guid CookingId { get; private set; }

    public Guid IngredientId { get; private set; }

    // Base units: the recipe's amount scaled to the servings and rounded (US-4).
    public decimal RequiredQuantity { get; private set; }

    // Base units: what the user confirmed; may be more than required (within the stock), never negative.
    public decimal UsedQuantity { get; private set; }

    // CONTEXT.md, Hiány: not booked, only shown.
    public decimal Shortage => Math.Max(0, RequiredQuantity - UsedQuantity);

    // This cooking used up the household's last stock of the ingredient ("Elfogyott"). Stored, so a repeated
    // request gets the same answer even after a later purchase (ADR-0008).
    public bool Depleted { get; private set; }

    // M5: the cooking made a shopping suggestion for the ingredient ("Felvettem a bevásárlójavaslatok közé: …").
    public bool Suggested { get; private set; }

    internal static CookingLine Create(Guid cookingId, Guid ingredientId, decimal required, decimal used, bool depleted) =>
        new(cookingId, ingredientId, required, used, depleted);
}
