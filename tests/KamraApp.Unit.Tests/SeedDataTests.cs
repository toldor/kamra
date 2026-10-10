using System.Text.Json;
using KamraApp.Domain.Ingredients;
using KamraApp.Infrastructure.Persistence.Seed;

namespace KamraApp.Unit.Tests;

// ADR-0005: system ingredients come from a JSON file with fixed ids and SeedKeys; the loader stops on
// inconsistent data instead of seeding it.
public class SeedDataTests
{
    [Fact]
    public void The_ingredient_list_loads_with_unique_ids_keys_and_names_and_the_staples()
    {
        var ingredients = SeedData.Ingredients;

        ingredients.Should().NotBeEmpty();
        ingredients.Should().OnlyContain(i => i.HouseholdId == null && i.Id.Version == 7 && i.SeedKey != null);
        ingredients.Select(i => i.Id).Should().OnlyHaveUniqueItems();
        ingredients.Select(i => i.SeedKey).Should().OnlyHaveUniqueItems();
        ingredients.Select(i => i.NormalizedName).Should().OnlyHaveUniqueItems();
        ingredients.Where(i => i.IsStaple).Select(i => i.SeedKey).Should().BeEquivalentTo("viz", "so", "bors");
    }

    [Fact]
    public void A_duplicate_seed_key_stops_the_load()
    {
        const string json = """
            [
              { "id": "0199c9a0-0000-7000-8000-000000000001", "seedKey": "tej", "name": "tej", "dimension": "Volume", "defaultCategory": "Dairy" },
              { "id": "0199c9a0-0000-7000-8000-000000000002", "seedKey": "tej", "name": "tej 2", "dimension": "Volume", "defaultCategory": "Dairy" }
            ]
            """;

        var load = () => SeedData.LoadIngredients(json);

        load.Should().Throw<InvalidOperationException>().WithMessage("*tej*");
    }

    [Theory]
    [InlineData("", "tej")]
    [InlineData("tej", " ")]
    public void A_blank_seed_key_or_name_stops_the_load(string seedKey, string name)
    {
        var json = $$"""
            [ { "id": "0199c9a0-0000-7000-8000-000000000001", "seedKey": "{{seedKey}}", "name": "{{name}}", "dimension": "Volume", "defaultCategory": "Dairy" } ]
            """;

        var load = () => SeedData.LoadIngredients(json);

        load.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void An_unknown_category_stops_the_load()
    {
        const string json = """
            [ { "id": "0199c9a0-0000-7000-8000-000000000001", "seedKey": "tej", "name": "tej", "dimension": "Volume", "defaultCategory": "Milk" } ]
            """;

        var load = () => SeedData.LoadIngredients(json);

        load.Should().Throw<JsonException>();
    }

    [Fact]
    public void Names_are_normalized_for_uniqueness()
    {
        Ingredient.Normalize("  Őrölt   Pirospaprika ").Should().Be("őrölt pirospaprika");
    }
}
