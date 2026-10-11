using KamraApp.Domain.Pantry;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Domain.Cookings;

// CONTEXT.md, Főzés: a recipe cooked for a number of servings. Each line keeps what the recipe needed
// (scaled) and what was really used; only the used amount leaves the pantry (US-4).
public sealed class Cooking
{
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
        int servings, Guid requestId, IReadOnlyDictionary<Guid, decimal> used, IReadOnlyList<PantryItem> items, DateTimeOffset now) =>
        throw new NotImplementedException();

    // ADR-0008: a repeated request is the same cooking only with the same recipe, servings and every used amount.
    public bool Matches(Guid recipeId, int servings, IReadOnlyDictionary<Guid, decimal> used) => throw new NotImplementedException();
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

    internal static CookingLine Create(Guid cookingId, Guid ingredientId, decimal required, decimal used, bool depleted) =>
        new(cookingId, ingredientId, required, used, depleted);
}
