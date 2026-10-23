using System.Text.Json;
using System.Text.Json.Serialization;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recipes;

namespace KamraApp.Infrastructure.Persistence.Seed;

// ADR-0005: system data lives in embedded JSON files with fixed ids and SeedKeys and reaches the
// database only through migrations (HasData). Inconsistent data stops the load.
public static class SeedData
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // Enum names only (no numbers), and every field must be present.
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    // Fixed, so regenerating the migration does not change the seeded rows.
    public static readonly DateTimeOffset SeededAt = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);

    private static readonly Lazy<IReadOnlyList<Ingredient>> SystemIngredients =
        new(() => LoadIngredients(ReadResource("ingredients.json")));

    private static readonly Lazy<SeedRecipes> SystemRecipes =
        new(() => LoadRecipes(ReadResource("recipes.json"), Ingredients));

    public static IReadOnlyList<Ingredient> Ingredients => SystemIngredients.Value;

    public static SeedRecipes Recipes => SystemRecipes.Value;

    // Recipe ingredients refer to the ingredient list by SeedKey (ADR-0005); an unknown key, a unit of
    // another dimension (ADR-0002) or a repeated ingredient stops the load.
    public static SeedRecipes LoadRecipes(string json, IReadOnlyList<Ingredient> ingredients)
    {
        var rows = JsonSerializer.Deserialize<List<RecipeRow>>(json, Options) ?? [];
        if (rows.Any(r => string.IsNullOrWhiteSpace(r.SeedKey) || string.IsNullOrWhiteSpace(r.Name)))
        {
            throw new InvalidOperationException("Blank seedKey or name in the recipe seed.");
        }

        EnsureUnique(rows, r => r.Id.ToString(), "recipe id");
        EnsureUnique(rows, r => r.SeedKey, "recipe seedKey");
        var bySeedKey = ingredients.Where(i => i.SeedKey is not null).ToDictionary(i => i.SeedKey!);

        var recipes = new List<Recipe>();
        var recipeIngredients = new List<RecipeIngredient>();
        foreach (var row in rows)
        {
            recipes.Add(Recipe.CreateSystem(row.Id, row.SeedKey, row.Name, row.Servings, row.Instructions, SeededAt));
            EnsureUnique(row.Ingredients, i => i.SeedKey, $"ingredient of recipe {row.SeedKey}");
            foreach (var line in row.Ingredients)
            {
                var ingredient = bySeedKey.GetValueOrDefault(line.SeedKey)
                    ?? throw new InvalidOperationException($"Unknown ingredient seedKey in recipe {row.SeedKey}: {line.SeedKey}");
                if (Units.DimensionOf(line.Unit) != ingredient.Dimension)
                {
                    throw new InvalidOperationException($"Unit {line.Unit} does not measure {ingredient.Dimension} for {line.SeedKey} in recipe {row.SeedKey}");
                }

                recipeIngredients.Add(RecipeIngredient.Create(row.Id, ingredient, line.Amount, line.Unit));
            }
        }

        return new SeedRecipes(recipes, recipeIngredients);
    }

    public static IReadOnlyList<Ingredient> LoadIngredients(string json)
    {
        var rows = JsonSerializer.Deserialize<List<IngredientRow>>(json, Options) ?? [];
        if (rows.Any(r => string.IsNullOrWhiteSpace(r.SeedKey) || string.IsNullOrWhiteSpace(r.Name)))
        {
            throw new InvalidOperationException("Blank seedKey or name in the seed data.");
        }

        EnsureUnique(rows, r => r.Id.ToString(), "id");
        EnsureUnique(rows, r => r.SeedKey, "seedKey");
        EnsureUnique(rows, r => Ingredient.Normalize(r.Name), "name");

        return [.. rows.Select(r => Ingredient.CreateSystem(r.Id, r.SeedKey, r.Name, r.Dimension, r.DefaultCategory))];
    }

    private static void EnsureUnique<T>(IEnumerable<T> rows, Func<T, string> key, string field)
    {
        var duplicate = rows.GroupBy(key).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate {field} in the seed data: {duplicate.Key}");
        }
    }

    private static string ReadResource(string fileName)
    {
        using var stream = typeof(SeedData).Assembly.GetManifestResourceStream($"Kamra.Seed.{fileName}")
            ?? throw new InvalidOperationException($"Missing embedded seed file: {fileName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed record RecipeRow(Guid Id, string SeedKey, string Name, int Servings, string Instructions, List<RecipeLineRow> Ingredients);

    private sealed record RecipeLineRow(string SeedKey, decimal Amount, Unit Unit);

    private sealed record IngredientRow(Guid Id, string SeedKey, string Name, Dimension Dimension, Category DefaultCategory);
}

public sealed record SeedRecipes(IReadOnlyList<Recipe> Recipes, IReadOnlyList<RecipeIngredient> Ingredients);
