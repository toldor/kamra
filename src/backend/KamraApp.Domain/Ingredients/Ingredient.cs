using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.Ingredients;

// ADR-0003: canonical ingredient, either from the system list (HouseholdId null, fixed SeedKey) or a
// household's own one; there is no hierarchy between ingredients.
public sealed class Ingredient
{
    private Ingredient(Guid id, Guid? householdId, string name, string normalizedName, Dimension dimension,
        Category defaultCategory, string? seedKey, DateTimeOffset? retiredAt)
    {
        Id = id;
        HouseholdId = householdId;
        Name = name;
        NormalizedName = normalizedName;
        Dimension = dimension;
        DefaultCategory = defaultCategory;
        SeedKey = seedKey;
        RetiredAt = retiredAt;
    }

    public Guid Id { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public string Name { get; private set; }

    public string NormalizedName { get; private set; }

    public Dimension Dimension { get; private set; }

    public Category DefaultCategory { get; private set; }

    public string? SeedKey { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    // CONTEXT.md, Alaphozzávaló: water, salt and pepper are ignored by matching and deduction.
    public bool IsStaple => IsStapleKey(SeedKey);

    public static bool IsStapleKey(string? seedKey) => seedKey is "viz" or "so" or "bors";

    public static Ingredient CreateSystem(Guid id, string seedKey, string name, Dimension dimension, Category defaultCategory) =>
        new(id, householdId: null, name, Normalize(name), dimension, defaultCategory, seedKey, retiredAt: null);

    public const int MaxNameLength = 100;

    // M11: a household's own ingredient; the use case has checked that the name is free.
    public static Ingredient CreateOwn(Guid householdId, string name, Dimension dimension, Category defaultCategory) =>
        throw new NotImplementedException();

    // The stored form of a name: no leading or trailing white space, single spaces inside (any Unicode white space).
    public static string Tidy(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // Lower case with single spaces, the form used for name uniqueness (data_model.md).
    public static string Normalize(string name) => Tidy(name).ToLowerInvariant();
}
