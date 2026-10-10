using System.Text.Json;
using System.Text.Json.Serialization;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

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

    private static readonly Lazy<IReadOnlyList<Ingredient>> SystemIngredients =
        new(() => LoadIngredients(ReadResource("ingredients.json")));

    public static IReadOnlyList<Ingredient> Ingredients => SystemIngredients.Value;

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

    private sealed record IngredientRow(Guid Id, string SeedKey, string Name, Dimension Dimension, Category DefaultCategory);
}
