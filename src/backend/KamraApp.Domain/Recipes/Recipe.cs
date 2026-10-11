using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Recipes;

// CONTEXT.md, Recept. A system recipe (HouseholdId null, fixed SeedKey) or, from okt. 23, a household's
// own one; quantities are for the default servings, in base units (ADR-0002).
public sealed class Recipe
{
    private Recipe(Guid id, Guid? householdId, string name, int servings, string instructions, RecipeSource source,
        string? seedKey, DateTimeOffset createdAt, DateTimeOffset? retiredAt)
    {
        Id = id;
        HouseholdId = householdId;
        Name = name;
        Servings = servings;
        Instructions = instructions;
        Source = source;
        SeedKey = seedKey;
        CreatedAt = createdAt;
        RetiredAt = retiredAt;
    }

    public Guid Id { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public string Name { get; private set; }

    public int Servings { get; private set; }

    public string Instructions { get; private set; }

    public RecipeSource Source { get; private set; }

    public string? SeedKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public static Recipe CreateSystem(Guid id, string seedKey, string name, int servings, string instructions, DateTimeOffset createdAt) =>
        throw new NotImplementedException();
}

public enum RecipeSource
{
    Seed,
    User,
}

// One ingredient of a recipe, once per recipe; the quantity is for the recipe's default servings.
public sealed class RecipeIngredient
{
    private RecipeIngredient(Guid recipeId, Guid ingredientId, decimal quantity, Unit enteredUnit)
    {
        RecipeId = recipeId;
        IngredientId = ingredientId;
        Quantity = quantity;
        EnteredUnit = enteredUnit;
    }

    public Guid RecipeId { get; private set; }

    public Guid IngredientId { get; private set; }

    // Base units (g, ml, db), > 0.
    public decimal Quantity { get; private set; }

    public Unit EnteredUnit { get; private set; }

    public static RecipeIngredient Create(Guid recipeId, Guid ingredientId, decimal amount, Unit unit) =>
        throw new NotImplementedException();
}
