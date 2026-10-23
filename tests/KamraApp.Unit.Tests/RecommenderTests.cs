using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-3 recommendation and the FEFO split it shares with cooking (US-4). The recipes are the approved
// seed recipes (S4 mini-spec); P5 and P7 are the hand-calculated examples (test_report.md).
public class RecommenderTests
{
    private static readonly DateOnly T = new(2026, 10, 11);
    private static readonly Dictionary<string, Guid> Ids = [];

    private static Guid Id(string ingredient)
    {
        if (!Ids.TryGetValue(ingredient, out var id))
        {
            id = Guid.CreateVersion7();
            Ids[ingredient] = id;
        }

        return id;
    }

    private static RecipeNeed Need(string ingredient, decimal amount, Unit unit, bool staple = false) =>
        new(Id(ingredient), ingredient, Units.ToBase(amount, unit), unit, staple);

    private static StockItem Stock(string ingredient, decimal baseQuantity, int daysToExpiry) =>
        new(Guid.CreateVersion7(), Id(ingredient), baseQuantity, T.AddDays(daysToExpiry));

    private static readonly RecipeCandidate Lecso = new(Guid.CreateVersion7(), "Lecsó", 4,
    [
        Need("paprika", 500, Unit.G), Need("paradicsom", 300, Unit.G), Need("vöröshagyma", 150, Unit.G),
        Need("étolaj", 30, Unit.Ml), Need("só", 5, Unit.G, staple: true),
    ]);

    private static readonly RecipeCandidate TurosCsusza = new(Guid.CreateVersion7(), "Túrós csusza", 4,
    [
        Need("száraztészta", 400, Unit.G), Need("túró", 500, Unit.G), Need("tejföl", 200, Unit.G),
        Need("szalonna", 150, Unit.G), Need("só", 10, Unit.G, staple: true),
    ]);

    private static readonly RecipeCandidate Palacsinta = new(Guid.CreateVersion7(), "Palacsinta", 4,
    [
        Need("finomliszt", 200, Unit.G), Need("tej", 5, Unit.Dl), Need("tojás", 2, Unit.Db), Need("étolaj", 30, Unit.Ml),
        Need("víz", 1, Unit.Dl, staple: true), Need("só", 2, Unit.G, staple: true),
    ]);

    private static readonly RecipeCandidate Rantotta = new(Guid.CreateVersion7(), "Rántotta", 2,
    [
        Need("tojás", 3, Unit.Db), Need("vaj", 20, Unit.G), Need("só", 2, Unit.G, staple: true), Need("bors", 1, Unit.G, staple: true),
    ]);

    private static readonly RecipeCandidate PaprikasKrumpli = new(Guid.CreateVersion7(), "Paprikás krumpli", 4,
    [
        Need("burgonya", 1, Unit.Kg), Need("vöröshagyma", 150, Unit.G), Need("kolbász", 150, Unit.G),
        Need("őrölt pirospaprika", 10, Unit.G), Need("étolaj", 30, Unit.Ml), Need("só", 10, Unit.G, staple: true),
        Need("víz", 5, Unit.Dl, staple: true),
    ]);

    private static readonly RecipeCandidate[] AllRecipes = [Lecso, TurosCsusza, Palacsinta, Rantotta, PaprikasKrumpli];

    [Fact]
    public void A_recipe_is_makeable_without_its_staples_in_stock()
    {
        var result = Recommender.Recommend([Rantotta], [Stock("tojás", 6, 10), Stock("vaj", 100, 20)], T);

        result.Makeable.Should().ContainSingle(r => r.Name == "Rántotta");
        result.Makeable[0].Shortages.Should().BeEmpty();
    }

    [Fact]
    public void A_recipe_of_staples_only_is_makeable_from_any_non_empty_pantry()
    {
        // CONTEXT.md, Elkészíthető: every ingredient except the staples is in stock - vacuously true here.
        var saltWater = new RecipeCandidate(Guid.CreateVersion7(), "Sós víz", 1, [Need("víz", 2, Unit.Dl, staple: true), Need("só", 1, Unit.G, staple: true)]);

        var result = Recommender.Recommend([saltWater], [Stock("rizs", 500, 90)], T);

        var water = result.Makeable.Single();
        water.Shortages.Should().BeEmpty();
        water.SoonExpiringIngredients.Should().BeEmpty();
        water.ExpiredIngredients.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void One_or_two_shortages_make_a_recipe_almost_makeable_three_leave_it_out(int missing, bool listed)
    {
        // Palacsinta has 4 non-staple ingredients; the first `missing` of them are not in stock.
        var stock = new[] { Stock("finomliszt", 1000, 90), Stock("tej", 1000, 7), Stock("tojás", 10, 21), Stock("étolaj", 1000, 90) }
            .Skip(missing);

        var result = Recommender.Recommend([Palacsinta], stock, T);

        result.Makeable.Should().BeEmpty();
        result.AlmostMakeable.Should().HaveCount(listed ? 1 : 0);
        if (listed)
        {
            result.AlmostMakeable[0].Shortages.Should().HaveCount(missing);
        }
    }

    [Fact]
    public void P5_a_missing_amount_is_shown_with_what_there_is_and_what_is_needed()
    {
        var stock = new[] { Stock("száraztészta", 400, 90), Stock("túró", 500, 5), Stock("szalonna", 150, 7), Stock("tejföl", 150, 6) };

        var result = Recommender.Recommend([TurosCsusza], stock, T);

        result.AlmostMakeable.Should().ContainSingle();
        result.AlmostMakeable[0].Shortages.Should().Equal(new Shortage(Id("tejföl"), "tejföl", 50m, 150m, 200m, Unit.G));
    }

    [Fact]
    public void Shortages_are_in_the_recipe_unit()
    {
        var stock = new[] { Stock("finomliszt", 1000, 90), Stock("tej", 300, 7), Stock("tojás", 10, 21), Stock("étolaj", 1000, 90) };

        var result = Recommender.Recommend([Palacsinta], stock, T);

        result.AlmostMakeable[0].Shortages.Should().Equal(new Shortage(Id("tej"), "tej", 2m, 3m, 5m, Unit.Dl));
    }

    [Fact]
    public void Several_pantry_items_of_an_ingredient_add_up()
    {
        var stock = new[]
        {
            Stock("száraztészta", 400, 90), Stock("túró", 500, 5), Stock("szalonna", 150, 7), Stock("tejföl", 120, 6), Stock("tejföl", 80, 9),
        };

        Recommender.Recommend([TurosCsusza], stock, T).Makeable.Should().ContainSingle(r => r.Name == "Túrós csusza");
    }

    [Fact]
    public void An_expired_item_is_stock_marked_as_expired_and_does_not_rank_a_recipe_up()
    {
        // Q5/E1: Rántotta uses an expired egg (the earliest date of all) and butter expiring in 20 days;
        // Tejfölös uses sour cream expiring in 5 days; neither is soon expiring. The expired egg must not
        // move Rántotta ahead (key 2 looks only at items that have not expired: 20 vs 5 days), and by name
        // "Rántotta" would come first, so only that rule can put Tejfölös ahead.
        var tejfolos = new RecipeCandidate(Guid.CreateVersion7(), "Tejfölös", 2, [Need("tejföl", 10, Unit.G)]);
        var stock = new[] { Stock("tojás", 3, -1), Stock("vaj", 100, 20), Stock("tejföl", 10, 5) };

        var result = Recommender.Recommend([Rantotta, tejfolos], stock, T);

        result.Makeable.Select(r => r.Name).Should().Equal("Tejfölös", "Rántotta");
        var rantotta = result.Makeable[1];
        rantotta.ExpiredIngredients.Should().Equal("tojás");
        rantotta.SoonExpiringIngredients.Should().BeEmpty();
    }

    [Fact]
    public void The_ranking_counts_soon_expiring_items_and_the_reason_names_the_ingredients()
    {
        // Two soon-expiring items of one ingredient: rank key 1 counts 2 items (US-3 "készlettételek
        // száma"), the reason lists the ingredient once ("1 hamarosan lejáró hozzávalót").
        var zoldseges = new RecipeCandidate(Guid.CreateVersion7(), "Zöldséges", 2, [Need("paradicsom", 300, Unit.G)]);
        var agyas = new RecipeCandidate(Guid.CreateVersion7(), "Ágyas", 2, [Need("paprika", 100, Unit.G)]);
        var stock = new[] { Stock("paradicsom", 200, 1), Stock("paradicsom", 200, 2), Stock("paprika", 100, 1) };

        var result = Recommender.Recommend([agyas, zoldseges], stock, T);

        result.Makeable.Select(r => r.Name).Should().Equal("Zöldséges", "Ágyas");
        result.Makeable[0].SoonExpiringIngredients.Should().Equal("paradicsom");
    }

    [Fact]
    public void Only_the_items_the_FEFO_split_uses_count_as_used()
    {
        // D2: the expired 300 g covers the need, so the soon-expiring 300 g is not used.
        var stock = new[]
        {
            Stock("paprika", 500, 20), Stock("paradicsom", 300, -1), Stock("paradicsom", 300, 1), Stock("vöröshagyma", 150, 20), Stock("étolaj", 100, 90),
        };

        var lecso = Recommender.Recommend([Lecso], stock, T).Makeable.Single();

        lecso.ExpiredIngredients.Should().Equal("paradicsom");
        lecso.SoonExpiringIngredients.Should().BeEmpty();
    }

    [Fact]
    public void P7_ranks_the_recipe_that_uses_soon_expiring_items_first()
    {
        var stock = new[]
        {
            Stock("paprika", 500, 2), Stock("paradicsom", 300, 1), Stock("vöröshagyma", 300, 10), Stock("étolaj", 1000, 90),
            Stock("burgonya", 1000, 30), Stock("kolbász", 150, 7), Stock("őrölt pirospaprika", 100, 90),
        };

        var result = Recommender.Recommend(AllRecipes, stock, T);

        result.Makeable.Select(r => r.Name).Should().Equal("Lecsó", "Paprikás krumpli");
        result.Makeable[0].SoonExpiringIngredients.Should().Equal("paradicsom", "paprika");
        result.Makeable[1].SoonExpiringIngredients.Should().BeEmpty();
        result.AlmostMakeable.Select(r => r.Name).Should().Equal("Rántotta");
        result.AlmostMakeable[0].Shortages.Should().Equal(
            new Shortage(Id("tojás"), "tojás", 3m, 0m, 3m, Unit.Db),
            new Shortage(Id("vaj"), "vaj", 20m, 0m, 20m, Unit.G));
    }

    [Fact]
    public void More_soon_expiring_items_used_rank_first_even_against_an_earlier_expiry_and_name()
    {
        // Rank key 1 alone: "Ágyas" uses butter expiring today (1 soon-expiring item, the earliest
        // not-expired date) and comes first by name; "Zöldséges" uses two soon-expiring items (T+1, T+2).
        // Keys 2 and 3 both favour "Ágyas", so only key 1 (2 > 1) can put "Zöldséges" first. (The first
        // version used an expired butter, which after E1 no longer counts for key 2 - V-30.)
        var agyas = new RecipeCandidate(Guid.CreateVersion7(), "Ágyas", 2, [Need("vaj", 10, Unit.G)]);
        var zoldseges = new RecipeCandidate(Guid.CreateVersion7(), "Zöldséges", 2, [Need("paprika", 100, Unit.G), Need("paradicsom", 100, Unit.G)]);
        var stock = new[] { Stock("vaj", 100, 0), Stock("paprika", 100, 1), Stock("paradicsom", 100, 2) };

        var result = Recommender.Recommend([agyas, zoldseges], stock, T);

        result.Makeable.Select(r => r.Name).Should().Equal("Zöldséges", "Ágyas");
    }

    [Fact]
    public void Equal_soon_expiring_counts_rank_by_the_earliest_expiry_then_by_Hungarian_name()
    {
        var bab = new RecipeCandidate(Guid.CreateVersion7(), "Bableves", 2, [Need("tojás", 1, Unit.Db)]);
        var agyas = new RecipeCandidate(Guid.CreateVersion7(), "Ágyas tojás", 2, [Need("tojás", 1, Unit.Db)]);
        var later = new RecipeCandidate(Guid.CreateVersion7(), "Aszalt", 2, [Need("vaj", 10, Unit.G)]);
        var stock = new[] { Stock("tojás", 10, 5), Stock("vaj", 100, 20) };

        var result = Recommender.Recommend([later, bab, agyas], stock, T);

        // Same expiry for the two egg recipes: Hungarian order puts "Ágyas" before "Bableves".
        result.Makeable.Select(r => r.Name).Should().Equal("Ágyas tojás", "Bableves", "Aszalt");
    }

    [Fact]
    public void Almost_makeable_recipes_with_fewer_shortages_come_first()
    {
        // D1: fewer shortages first. Rántotta misses only the butter (1), Palacsinta the flour and the milk
        // (2); by name Palacsinta would come first, so only the shortage count can put Rántotta ahead.
        var stock = new[] { Stock("tojás", 3, 1), Stock("étolaj", 1000, 90) };

        var result = Recommender.Recommend([Palacsinta, Rantotta], stock, T);

        result.Makeable.Should().BeEmpty();
        result.AlmostMakeable.Select(r => r.Name).Should().Equal("Rántotta", "Palacsinta");
    }

    [Fact]
    public void Almost_makeable_recipes_with_equal_shortages_rank_by_soon_expiring_items_used()
    {
        // D1 key 2: both miss one ingredient; "Zöldséges" uses a soon-expiring item, "Ágyas" does not, so
        // "Zöldséges" comes first although "Ágyas" is first by name.
        var agyas = new RecipeCandidate(Guid.CreateVersion7(), "Ágyas", 2, [Need("vaj", 10, Unit.G), Need("sajt", 50, Unit.G)]);
        var zoldseges = new RecipeCandidate(Guid.CreateVersion7(), "Zöldséges", 2, [Need("paprika", 100, Unit.G), Need("rizs", 100, Unit.G)]);
        var stock = new[] { Stock("vaj", 100, 20), Stock("paprika", 100, 1) };

        var result = Recommender.Recommend([agyas, zoldseges], stock, T);

        result.AlmostMakeable.Select(r => r.Name).Should().Equal("Zöldséges", "Ágyas");
    }

    [Fact]
    public void The_result_does_not_depend_on_the_order_of_the_input()
    {
        var stock = new[]
        {
            Stock("paprika", 500, 2), Stock("paradicsom", 300, 1), Stock("vöröshagyma", 300, 10), Stock("étolaj", 1000, 90),
            Stock("burgonya", 1000, 30), Stock("kolbász", 150, 7), Stock("őrölt pirospaprika", 100, 90), Stock("tojás", 2, 0),
        };
        var expected = Describe(Recommender.Recommend(AllRecipes, stock, T));

        for (var seed = 0; seed < 20; seed++)
        {
            var random = new Random(seed);
            var shuffled = Describe(Recommender.Recommend(AllRecipes.OrderBy(_ => random.Next()), stock.OrderBy(_ => random.Next()), T));
            shuffled.Should().Be(expected);
        }
    }

    [Fact]
    public void A_different_ingredient_never_matches_even_with_a_similar_name()
    {
        // ADR-0003: no hierarchy - "trappista sajt" in stock does not satisfy a recipe that needs "sajt".
        var sajtos = new RecipeCandidate(Guid.CreateVersion7(), "Sajtos tészta", 2, [Need("sajt", 100, Unit.G)]);

        var result = Recommender.Recommend([sajtos], [Stock("trappista sajt", 500, 20)], T);

        result.Makeable.Should().BeEmpty();
        result.AlmostMakeable.Single().Shortages.Should().Equal(new Shortage(Id("sajt"), "sajt", 100m, 0m, 100m, Unit.G));
    }

    [Fact]
    public void Fefo_takes_the_earliest_expiring_item_first()
    {
        // P2: 200 g (T+1) and 400 g (T+5), 300 g needed -> 200 g from the first, 100 g from the second.
        var first = Stock("paradicsom", 200, 1);
        var second = Stock("paradicsom", 400, 5);

        Fefo.Allocate([second, first], 300).Should().Equal((first, 200m), (second, 100m));
    }

    [Fact]
    public void Fefo_allocates_at_most_what_there_is_and_splits_ties_by_id()
    {
        var a = Stock("tojás", 2, 3);
        var b = Stock("tojás", 2, 3);
        var (earlier, later) = a.PantryItemId.CompareTo(b.PantryItemId) < 0 ? (a, b) : (b, a);

        Fefo.Allocate([later, earlier], 10).Should().Equal((earlier, 2m), (later, 2m));
    }

    private static string Describe(Recommendations result) =>
        string.Join(" | ", result.Makeable.Concat(result.AlmostMakeable).Select(r =>
            $"{r.Name}: {string.Join(",", r.SoonExpiringIngredients)} / {string.Join(",", r.ExpiredIngredients)} / " +
            string.Join(",", r.Shortages.Select(s => $"{s.IngredientName}={s.Missing}"))));
}
