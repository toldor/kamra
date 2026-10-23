using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recipes;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// ADR-0004: the Domain keeps its invariants regardless of who calls it (the seed today, own recipes
// from okt. 23).
public class RecipeTests
{
    private static readonly Ingredient Milk = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);

    [Fact]
    public void A_recipe_ingredient_is_stored_in_base_units()
    {
        var line = RecipeIngredient.Create(Guid.CreateVersion7(), Milk, 5m, Unit.Dl);

        line.IngredientId.Should().Be(Milk.Id);
        line.Quantity.Should().Be(500m);
        line.EnteredUnit.Should().Be(Unit.Dl);
    }

    [Fact]
    public void A_recipe_ingredient_in_a_unit_of_another_dimension_is_rejected()
    {
        var create = () => RecipeIngredient.Create(Guid.CreateVersion7(), Milk, 200m, Unit.G);

        create.Should().Throw<ArgumentException>("ADR-0002: no conversion between mass and volume");
    }
}
