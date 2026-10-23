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
    public void The_recipe_list_loads_the_twenty_recipes_on_known_ingredients()
    {
        var ingredientIds = SeedData.Ingredients.Select(i => i.Id).ToHashSet();

        // The first five (Oct 16 milestone) unchanged, so P1-P10 keep their data; fifteen added (O3, seed review).
        SeedData.Recipes.Recipes.Select(r => r.SeedKey).Should().BeEquivalentTo(
            "lecso", "turos-csusza", "palacsinta", "rantotta", "paprikas-krumpli",
            "rakott-krumpli", "gulyasleves", "csirkepaprikas", "bolognai-spagetti", "rantott-csirkemell-rizzsel",
            "zoldborsofozelek", "krumplifozelek", "sajtos-sonkas-teszta", "bableves", "rizibizi",
            "tejbegriz", "gombapaprikas", "paradicsomleves", "toltott-paprika", "lencsefozelek");
        SeedData.Recipes.Recipes.Should().OnlyContain(r => r.Id.Version == 7 && r.HouseholdId == null && r.Servings > 0);
        SeedData.Recipes.Ingredients.Should().OnlyContain(i => ingredientIds.Contains(i.IngredientId) && i.Quantity > 0);
        SeedData.Recipes.Ingredients.Select(i => (i.RecipeId, i.IngredientId)).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void An_unknown_ingredient_seed_key_stops_the_recipe_load()
    {
        // ADR-0005: seed files refer to each other by SeedKey; an unknown key stops the load.
        var json = Recipe("""{ "seedKey": "sarkanygyokér", "amount": 1, "unit": "db" }""");

        var load = () => SeedData.LoadRecipes(json, SeedData.Ingredients);

        load.Should().Throw<InvalidOperationException>().WithMessage("*sarkanygyokér*");
    }

    [Fact]
    public void A_unit_from_another_dimension_stops_the_recipe_load()
    {
        var json = Recipe("""{ "seedKey": "tej", "amount": 200, "unit": "g" }""");

        var load = () => SeedData.LoadRecipes(json, SeedData.Ingredients);

        load.Should().Throw<InvalidOperationException>().WithMessage("*tej*");
    }

    private static string Recipe(string ingredient) => $$"""
        [ { "id": "0199c9a0-0000-7000-8000-000000000009", "seedKey": "proba", "name": "Próba", "servings": 2,
            "instructions": "Összekeverjük.", "ingredients": [ {{ingredient}} ] } ]
        """;

    [Fact]
    public void Names_are_normalized_for_uniqueness()
    {
        Ingredient.Normalize("  Őrölt   Pirospaprika ").Should().Be("őrölt pirospaprika");
    }

    // scope_contract.md, 3. Korlátok / Adat: 20-40 recipes and about 150-250 ingredients.
    [Fact]
    public void The_starter_lists_are_within_the_committed_size()
    {
        SeedData.Recipes.Recipes.Count.Should().BeInRange(20, 40);
        SeedData.Ingredients.Count.Should().BeInRange(150, 250);
    }

    // Every starter recipe can be matched and cooked: at least one non-staple ingredient, servings within the
    // cooking limit (1-100), and a name of its own.
    [Fact]
    public void Every_starter_recipe_has_a_non_staple_ingredient_valid_servings_and_a_unique_name()
    {
        var staples = SeedData.Ingredients.Where(i => i.IsStaple).Select(i => i.Id).ToHashSet();

        SeedData.Recipes.Recipes.Should().OnlyContain(r => r.Servings >= 1 && r.Servings <= 100
            && SeedData.Recipes.Ingredients.Any(i => i.RecipeId == r.Id && !staples.Contains(i.IngredientId)));
        SeedData.Recipes.Recipes.Select(r => Ingredient.Normalize(r.Name)).Should().OnlyHaveUniqueItems();
    }
}
