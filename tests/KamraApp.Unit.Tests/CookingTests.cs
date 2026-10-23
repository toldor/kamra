using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-4 cooking in the Domain: scaling and rounding, FEFO deduction with a Consumed movement per item,
// shortage, and the repeated-request comparison of ADR-0008. P2, P3, P4, P6 and P10 are the
// hand-calculated examples of test_report.md.
public class CookingTests
{
    private static readonly DateOnly T = new(2026, 10, 11);
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();

    private static readonly Ingredient Paprika = Mass("paprika", "paprika");
    private static readonly Ingredient Paradicsom = Mass("paradicsom", "paradicsom");
    private static readonly Ingredient Voroshagyma = Mass("voroshagyma", "vöröshagyma");
    private static readonly Ingredient Etolaj = Ingredient.CreateSystem(Guid.CreateVersion7(), "etolaj", "étolaj", Dimension.Volume, Category.Other);
    private static readonly Ingredient So = Mass("so", "só");
    private static readonly Ingredient Tejfol = Mass("tejfol", "tejföl");
    private static readonly Ingredient Turo = Mass("turo", "túró");

    private static readonly RecipeCandidate Lecso = new(Guid.CreateVersion7(), "Lecsó", 4,
    [
        Need(Paprika, 500, Unit.G), Need(Paradicsom, 300, Unit.G), Need(Voroshagyma, 150, Unit.G), Need(Etolaj, 30, Unit.Ml),
        Need(So, 5, Unit.G, staple: true),
    ]);

    private static readonly RecipeCandidate Csusza = new(Guid.CreateVersion7(), "Túrós csusza", 4,
        [Need(Turo, 500, Unit.G), Need(Tejfol, 200, Unit.G), Need(So, 10, Unit.G, staple: true)]);

    private static Ingredient Mass(string key, string name) =>
        Ingredient.CreateSystem(Guid.CreateVersion7(), key, name, Dimension.Mass, Category.Vegetables);

    private static RecipeNeed Need(Ingredient ingredient, decimal amount, Unit unit, bool staple = false) =>
        new(ingredient.Id, ingredient.Name, Units.ToBase(amount, unit), unit, staple);

    private static PantryItem Item(Ingredient ingredient, decimal baseQuantity, int daysToExpiry)
    {
        var unit = ingredient.Dimension == Dimension.Volume ? Unit.Ml : Unit.G;
        return PantryItem.Create(HouseholdId, ingredient, baseQuantity, unit, Category.Vegetables, T.AddDays(daysToExpiry), T, Now).Item;
    }

    private static Dictionary<Guid, decimal> Used(params (Ingredient Ingredient, decimal Amount)[] amounts) =>
        amounts.ToDictionary(a => a.Ingredient.Id, a => a.Amount);

    // Lecsó with every non-staple ingredient; the tomatoes as given.
    private static Dictionary<Guid, decimal> LecsoUsed(decimal paradicsom) =>
        Used((Paprika, 0), (Paradicsom, paradicsom), (Voroshagyma, 0), (Etolaj, 0));

    [Theory]
    [InlineData(3, Unit.Db, 2, 3, 5)]        // P3: 4.5 eggs -> 5
    [InlineData(20, Unit.G, 2, 3, 30)]       // P3: butter
    [InlineData(200, Unit.G, 4, 6, 300)]     // P4: flour
    [InlineData(500, Unit.Dl, 4, 6, 750)]    // P4: 5 dl milk = 500 ml -> 750 ml
    [InlineData(2, Unit.Db, 4, 6, 3)]        // P4: eggs
    [InlineData(30, Unit.Ml, 4, 6, 45)]      // P4: oil
    [InlineData(5, Unit.G, 2, 1, 3)]         // 2.5 g -> 3: a half rounds up, not to even (banker's would give 2)
    [InlineData(15, Unit.G, 2, 1, 8)]        // 7.5 g -> 8
    [InlineData(41, Unit.Db, 10, 1, 5)]      // 4.1 pieces -> 5: pieces always round up
    [InlineData(1000, Unit.Kg, 4, 6, 1500)]
    public void Scaling_is_linear_g_and_ml_round_half_up_and_pieces_round_up(decimal baseQuantity, Unit unit, int recipeServings,
        int servings, decimal expected)
    {
        CookingCalculator.ScaleRequired(baseQuantity, unit, recipeServings, servings).Should().Be(expected);
    }

    [Fact]
    public void P2_the_earliest_expiring_item_is_used_first_with_a_consumed_movement_naming_the_cooking()
    {
        var soon = Item(Paradicsom, 200, 1);
        var later = Item(Paradicsom, 400, 5);

        var (cooking, movements) = Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(300), [later, soon], Now);

        soon.Quantity.Should().Be(0);
        later.Quantity.Should().Be(300);
        movements.Select(m => (m.PantryItemId, m.Delta)).Should().Equal((soon.Id, -200m), (later.Id, -100m));
        movements.Should().AllSatisfy(m =>
        {
            m.Reason.Should().Be(MovementReason.Consumed);
            m.CookingId.Should().Be(cooking.Id);
            m.HouseholdId.Should().Be(HouseholdId);
            m.OccurredAt.Should().Be(Now);
        });
        movements[0].ExpiryDateAtMovement.Should().Be(T.AddDays(1));
        var line = cooking.Lines.Single(l => l.IngredientId == Paradicsom.Id);
        (line.RequiredQuantity, line.UsedQuantity, line.Shortage, line.Depleted).Should().Be((300m, 300m, 0m, false));
    }

    [Fact]
    public void P6_only_the_used_amount_leaves_the_pantry_and_the_rest_is_a_shortage()
    {
        var tejfol = Item(Tejfol, 150, 6);
        var turo = Item(Turo, 500, 5);

        var (cooking, movements) = Cooking.Create(HouseholdId, Csusza, 4, Guid.CreateVersion7(), Used((Turo, 500), (Tejfol, 150)),
            [tejfol, turo], Now);

        tejfol.Quantity.Should().Be(0);
        movements.Where(m => m.PantryItemId == tejfol.Id).Select(m => m.Delta).Should().Equal(-150m);
        var line = cooking.Lines.Single(l => l.IngredientId == Tejfol.Id);
        (line.RequiredQuantity, line.UsedQuantity, line.Shortage, line.Depleted).Should().Be((200m, 150m, 50m, true));
    }

    [Fact]
    public void An_expired_item_is_used_before_a_fresh_one()
    {
        var expired = Item(Paradicsom, 100, -1);
        var fresh = Item(Paradicsom, 400, 3);

        Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(300), [fresh, expired], Now);

        expired.Quantity.Should().Be(0);
        fresh.Quantity.Should().Be(200);
    }

    [Fact]
    public void Using_more_than_required_within_the_stock_leaves_no_shortage()
    {
        var item = Item(Paradicsom, 500, 3);

        var (cooking, _) = Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(350), [item], Now);

        item.Quantity.Should().Be(150);
        cooking.Lines.Single(l => l.IngredientId == Paradicsom.Id).Shortage.Should().Be(0);
    }

    [Fact]
    public void Staples_never_become_lines_and_are_never_deducted()
    {
        var salt = Item(So, 1000, 300);
        var tomato = Item(Paradicsom, 300, 3);

        var (cooking, movements) = Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(300), [tomato, salt], Now);

        cooking.Lines.Select(l => l.IngredientId).Should().BeEquivalentTo([Paprika.Id, Paradicsom.Id, Voroshagyma.Id, Etolaj.Id]);
        salt.Quantity.Should().Be(1000);
        movements.Should().NotContain(m => m.PantryItemId == salt.Id);
    }

    [Fact]
    public void A_zero_used_amount_is_recorded_as_a_full_shortage_without_a_movement()
    {
        var onion = Item(Voroshagyma, 300, 10);
        var tomato = Item(Paradicsom, 300, 3);

        var (cooking, movements) = Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(300), [onion, tomato], Now);

        onion.Quantity.Should().Be(300);
        movements.Should().NotContain(m => m.PantryItemId == onion.Id);
        var line = cooking.Lines.Single(l => l.IngredientId == Voroshagyma.Id);
        (line.UsedQuantity, line.Shortage, line.Depleted).Should().Be((0m, 150m, false));
    }

    [Fact]
    public void The_cooking_keeps_the_household_recipe_servings_request_and_time()
    {
        var requestId = Guid.CreateVersion7();

        var (cooking, _) = Cooking.Create(HouseholdId, Lecso, 6, requestId, LecsoUsed(0), [], Now);

        cooking.Id.Version.Should().Be(7);
        (cooking.HouseholdId, cooking.RecipeId, cooking.Servings, cooking.RequestId, cooking.CookedAt)
            .Should().Be((HouseholdId, Lecso.RecipeId, 6, requestId, Now));
        cooking.Lines.Single(l => l.IngredientId == Paradicsom.Id).RequiredQuantity.Should().Be(450);
    }

    [Fact]
    public void P10_using_more_than_the_stock_is_refused_and_nothing_changes()
    {
        var onion = Item(Voroshagyma, 300, 10);
        var tomato = Item(Paradicsom, 200, 3);
        var used = Used((Paprika, 0), (Paradicsom, 250), (Voroshagyma, 100), (Etolaj, 0));

        var act = () => Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), used, [onion, tomato], Now);

        act.Should().Throw<InvalidOperationException>();
        onion.Quantity.Should().Be(300, "the check runs before any deduction");
        tomato.Quantity.Should().Be(200);
    }

    // ADR-0004: the servings limit (S5-D5) is the entity's own invariant, as PantryItem's quantity limit is:
    // it keeps the scaled amounts inside numeric(12,3).
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Servings_are_between_1_and_100(int servings)
    {
        var act = () => Cooking.Create(HouseholdId, Lecso, servings, Guid.CreateVersion7(), LecsoUsed(0), [], Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_used_amount_is_needed_for_every_non_staple_ingredient()
    {
        var act = () => Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), Used((Paradicsom, 0)), [], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    public void Consume_takes_a_positive_amount_up_to_what_the_item_holds(decimal amount)
    {
        var item = Item(Paradicsom, 200, 3);

        var act = () => item.Consume(amount, Guid.CreateVersion7(), Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
        item.Quantity.Should().Be(200);
    }

    [Fact]
    public void A_repeated_request_matches_only_the_same_recipe_servings_and_amounts()
    {
        var item = Item(Paradicsom, 600, 3);
        var (cooking, _) = Cooking.Create(HouseholdId, Lecso, 4, Guid.CreateVersion7(), LecsoUsed(300), [item], Now);

        cooking.Matches(Lecso.RecipeId, 4, LecsoUsed(300)).Should().BeTrue();
        cooking.Matches(Lecso.RecipeId, 4, LecsoUsed(300.000m)).Should().BeTrue("300 and 300.000 are the same amount");
        cooking.Matches(Csusza.RecipeId, 4, LecsoUsed(300)).Should().BeFalse();
        cooking.Matches(Lecso.RecipeId, 5, LecsoUsed(300)).Should().BeFalse();
        cooking.Matches(Lecso.RecipeId, 4, LecsoUsed(250)).Should().BeFalse();
        cooking.Matches(Lecso.RecipeId, 4, Used((Paradicsom, 300))).Should().BeFalse();
    }

    [Fact]
    public void The_preview_gives_required_available_default_used_and_the_FEFO_split()
    {
        var soon = StockItem.From(Item(Paradicsom, 200, 1));
        var later = StockItem.From(Item(Paradicsom, 400, 5));
        var salt = StockItem.From(Item(So, 1000, 300));

        var lines = CookingCalculator.Preview(Lecso, 4, [later, salt, soon]);

        lines.Select(l => l.Need.IngredientId).Should().Equal(Paprika.Id, Paradicsom.Id, Voroshagyma.Id, Etolaj.Id);
        var tomato = lines.Single(l => l.Need.IngredientId == Paradicsom.Id);
        (tomato.Required, tomato.Available, tomato.DefaultUsed).Should().Be((300m, 600m, 300m));
        tomato.Allocations.Select(a => (a.Item.PantryItemId, a.Amount)).Should().Equal((soon.PantryItemId, 200m), (later.PantryItemId, 100m));
        var pepper = lines.Single(l => l.Need.IngredientId == Paprika.Id);
        (pepper.Required, pepper.Available, pepper.DefaultUsed).Should().Be((500m, 0m, 0m));
        pepper.Allocations.Should().BeEmpty();
    }

    [Fact]
    public void The_preview_default_is_the_stock_when_it_is_less_than_required()
    {
        var lines = CookingCalculator.Preview(Csusza, 4, [StockItem.From(Item(Tejfol, 150, 6))]);

        var tejfol = lines.Single(l => l.Need.IngredientId == Tejfol.Id);
        (tejfol.Required, tejfol.Available, tejfol.DefaultUsed).Should().Be((200m, 150m, 150m));
    }
}
