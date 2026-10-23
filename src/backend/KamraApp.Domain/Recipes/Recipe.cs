using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
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

    public const int MaxNameLength = 100;
    public const int MaxIngredients = 50;
    public const int MaxInstructionsLength = 4000;

    public static Recipe CreateSystem(Guid id, string seedKey, string name, int servings, string instructions, DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(servings);
        return new Recipe(id, householdId: null, name, servings, instructions, RecipeSource.Seed, seedKey, createdAt, retiredAt: null);
    }

    // M9: a household's own recipe with its ingredient lines. The use case checks every rule first, with a
    // message per field; these guards keep the invariants for any other caller (ADR-0004).
    public static (Recipe Recipe, IReadOnlyList<RecipeIngredient> Ingredients) CreateOwn(Guid householdId, string name, int servings,
        string instructions, IReadOnlyList<RecipeLine> lines, DateTimeOffset createdAt)
    {
        var tidy = Ingredient.Tidy(name);
        if (tidy.Length is 0 or > MaxNameLength)
        {
            throw new ArgumentException("An own recipe needs a name of 1-100 characters.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(servings, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(servings, Cooking.MaxServings);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(instructions.Length, MaxInstructionsLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lines.Count, MaxIngredients);
        if (lines.Any(l => l.Ingredient.HouseholdId is { } owner && owner != householdId))
        {
            throw new ArgumentException("Another household's ingredient cannot go in this household's recipe.", nameof(lines));
        }

        if (lines.DistinctBy(l => l.Ingredient.Id).Count() != lines.Count)
        {
            throw new ArgumentException("An ingredient appears once per recipe.", nameof(lines));
        }

        if (lines.All(l => l.Ingredient.IsStaple))
        {
            throw new ArgumentException("An own recipe needs at least one non-staple ingredient.", nameof(lines));
        }

        var recipe = new Recipe(Guid.CreateVersion7(), householdId, tidy, servings, instructions, RecipeSource.User, seedKey: null, createdAt,
            retiredAt: null);
        return (recipe, [.. lines.Select(l => RecipeIngredient.Create(recipe.Id, l.Ingredient, l.Amount, l.Unit))]);
    }
}

// One requested line of an own recipe, in the unit the user entered.
public sealed record RecipeLine(Ingredient Ingredient, decimal Amount, Unit Unit);

// M10: a recipe archived in one household (any recipe, a system one too). It leaves the household's
// recommendations and cannot be cooked again; its cookings stay (data_model.md, RecipeArchives).
public sealed class RecipeArchive
{
    private RecipeArchive(Guid householdId, Guid recipeId, DateTimeOffset archivedAt)
    {
        HouseholdId = householdId;
        RecipeId = recipeId;
        ArchivedAt = archivedAt;
    }

    public Guid HouseholdId { get; private set; }

    public Guid RecipeId { get; private set; }

    public DateTimeOffset ArchivedAt { get; private set; }
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

    public static RecipeIngredient Create(Guid recipeId, Ingredient ingredient, decimal amount, Unit unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Units.EnsureDimension(unit, ingredient.Dimension);
        return new RecipeIngredient(recipeId, ingredient.Id, Units.ToBase(amount, unit), unit);
    }
}
