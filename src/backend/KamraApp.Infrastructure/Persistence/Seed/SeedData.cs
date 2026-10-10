using KamraApp.Domain.Ingredients;

namespace KamraApp.Infrastructure.Persistence.Seed;

// ADR-0005: system data lives in embedded JSON files with fixed ids and SeedKeys and reaches the
// database only through migrations (HasData). Inconsistent data stops the load.
public static class SeedData
{
    public static IReadOnlyList<Ingredient> Ingredients => throw new NotImplementedException();

    public static IReadOnlyList<Ingredient> LoadIngredients(string json) => throw new NotImplementedException();
}
