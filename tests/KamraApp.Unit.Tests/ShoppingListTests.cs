using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-5 in the Domain: when a suggestion is made or cleared (M1, M3, M6, D2), its states (ADR-0008 4.), merging
// list rows (M2, D3, M7, V-22) and the minimum. P12, P14, P15, P16 and P19 are the hand-calculated examples of
// the plan (kamra_okt22_terv.md, 5.), here at rule level; the API tests repeat them end to end.
public class ShoppingListTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient Tej = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);
    private static readonly Ingredient Tejfol = Ingredient.CreateSystem(Guid.CreateVersion7(), "tejfol", "tejföl", Dimension.Mass, Category.Dairy);

    [Theory]
    [InlineData(200, 0, null, MovementReason.Consumed, SuggestionTrigger.Depleted)]        // P14a
    [InlineData(600, 400, 500, MovementReason.Consumed, SuggestionTrigger.BelowMinimum)]  // P19: crosses the minimum
    [InlineData(1000, 300, 500, MovementReason.Consumed, SuggestionTrigger.BelowMinimum)] // P19: the second crossing
    [InlineData(600, 0, 500, MovementReason.Consumed, SuggestionTrigger.Depleted)]        // D2: one suggestion, "Elfogyott"
    [InlineData(500, 499, 500, MovementReason.Consumed, SuggestionTrigger.BelowMinimum)]  // from exactly the minimum
    public void A_consumed_decrease_suggests(int before, int after, int? minimum, MovementReason reason, SuggestionTrigger expected) =>
        SuggestionRules.OnDecrease(before, after, minimum, reason).Should().Be(expected);

    [Theory]
    [InlineData(200, 0, null, MovementReason.Discarded)]  // P14b: wasted, not used up
    [InlineData(200, 0, null, MovementReason.Corrected)]  // P14c: a wrong entry
    [InlineData(600, 400, 500, MovementReason.Discarded)]
    [InlineData(400, 300, 500, MovementReason.Consumed)]  // already below: no second suggestion
    [InlineData(1400, 1000, 500, MovementReason.Consumed)] // P19: still above the minimum
    [InlineData(300, 200, null, MovementReason.Consumed)]  // no minimum, not used up
    public void Other_decreases_do_not_suggest(int before, int after, int? minimum, MovementReason reason) =>
        SuggestionRules.OnDecrease(before, after, minimum, reason).Should().BeNull();

    [Theory]
    [InlineData(300, 500, true)]  // P19c (M1)
    [InlineData(0, 500, true)]
    [InlineData(500, 500, false)]
    [InlineData(600, 500, false)]
    public void Setting_a_minimum_above_the_total_suggests_at_once(int total, int minimum, bool suggests) =>
        SuggestionRules.OnMinimumSet(total, minimum).Should().Be(suggests ? SuggestionTrigger.BelowMinimum : null);

    [Theory]
    [InlineData(SuggestionTrigger.Depleted, 1000, null, true)]      // P15: bought again
    [InlineData(SuggestionTrigger.Depleted, 0, null, false)]
    [InlineData(SuggestionTrigger.BelowMinimum, 1400, 500, true)]   // P19
    [InlineData(SuggestionTrigger.BelowMinimum, 500, 500, true)]
    [InlineData(SuggestionTrigger.BelowMinimum, 400, 500, false)]
    [InlineData(SuggestionTrigger.BelowMinimum, 400, null, true)]   // the minimum was removed
    public void A_rejected_suggestion_clears_above_its_condition(SuggestionTrigger trigger, int total, int? minimum, bool clears) =>
        SuggestionRules.Clears(trigger, total, minimum).Should().Be(clears);

    [Theory]
    [InlineData(SuggestionTrigger.BelowMinimum, 400, 500, 100)]  // P19
    [InlineData(SuggestionTrigger.BelowMinimum, 300, 500, 200)]  // P19, P19c
    [InlineData(SuggestionTrigger.Depleted, 0, 500, null)]       // M13, D2: the user decides
    [InlineData(SuggestionTrigger.Depleted, 0, null, null)]
    public void The_suggested_amount_is_what_is_missing_up_to_the_minimum(SuggestionTrigger trigger, int total, int? minimum, int? expected) =>
        SuggestionRules.SuggestedQuantity(trigger, total, minimum).Should().Be(expected);

    [Fact]
    public void A_new_suggestion_is_open_and_blocks_the_next_one()
    {
        var suggestion = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);

        (suggestion.State, suggestion.Trigger, suggestion.CreatedAt, suggestion.Blocks).Should()
            .Be((SuggestionState.Open, SuggestionTrigger.Depleted, Now, true));
    }

    [Fact]
    public void Accepting_twice_is_a_repeat_and_rejecting_after_it_a_conflict()
    {
        var suggestion = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);

        suggestion.Accept(Now.AddMinutes(1)).Should().Be(SuggestionTransition.Changed);
        suggestion.Accept(Now.AddMinutes(2)).Should().Be(SuggestionTransition.Repeated);
        suggestion.Reject(Now.AddMinutes(3)).Should().Be(SuggestionTransition.Conflict);

        (suggestion.State, suggestion.ResolvedAt, suggestion.Blocks).Should().Be((SuggestionState.Accepted, (DateTimeOffset?)Now.AddMinutes(1), false));
    }

    [Fact]
    public void A_rejected_suggestion_blocks_until_it_is_cleared()
    {
        var suggestion = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);

        suggestion.Reject(Now).Should().Be(SuggestionTransition.Changed);
        suggestion.Reject(Now).Should().Be(SuggestionTransition.Repeated);
        suggestion.Accept(Now).Should().Be(SuggestionTransition.Conflict);
        suggestion.Blocks.Should().BeTrue();

        suggestion.Clear(Now.AddDays(1));

        (suggestion.State, suggestion.ClearedAt, suggestion.Blocks).Should().Be((SuggestionState.Rejected, (DateTimeOffset?)Now.AddDays(1), false));
    }

    [Fact]
    public void Only_a_rejected_suggestion_can_be_cleared()
    {
        var suggestion = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);

        suggestion.Invoking(s => s.Clear(Now)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_new_row_keeps_the_amount_in_its_unit()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 50, Unit.G, Now);

        (item.Quantity, item.Amount, item.EnteredUnit, item.Checked, item.AddedAt).Should().Be(((decimal?)50m, (decimal?)50m, (Unit?)Unit.G, false, Now));
    }

    [Fact]
    public void P12a_an_unchecked_row_adds_up()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 50, Unit.G, Now);

        item.TryAdd(100, Unit.G).Should().BeTrue();

        (item.Amount, item.EnteredUnit, item.Checked).Should().Be(((decimal?)150m, (Unit?)Unit.G, false));
    }

    [Fact]
    public void P12b_a_checked_row_starts_again_with_the_new_amount()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 50, Unit.G, Now);
        item.Update(isChecked: true, 50, Unit.G);

        item.TryAdd(50, Unit.G).Should().BeTrue();

        (item.Amount, item.EnteredUnit, item.Checked).Should().Be(((decimal?)50m, (Unit?)Unit.G, false));
    }

    [Theory]
    [InlineData(null, null, 200, Unit.G, 200)]  // P16: unknown, then known
    [InlineData(200, Unit.G, null, null, 200)]  // P16: known, then unknown
    [InlineData(null, null, null, null, null)]  // P16c
    public void P16_an_unknown_amount_keeps_the_known_one(int? first, Unit? firstUnit, int? second, Unit? secondUnit, int? expected)
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, first, firstUnit, Now);

        item.TryAdd(second, secondUnit).Should().BeTrue();

        (item.Amount, item.EnteredUnit).Should().Be(((decimal?)expected, expected is null ? (Unit?)null : Unit.G));
    }

    [Fact]
    public void Different_units_meet_in_the_smaller_one()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 1, Unit.Kg, Now);

        item.TryAdd(50, Unit.G).Should().BeTrue();

        (item.Quantity, item.Amount, item.EnteredUnit).Should().Be(((decimal?)1050m, (decimal?)1050m, (Unit?)Unit.G));
    }

    [Fact]
    public void An_addition_over_the_limit_is_refused_and_changes_nothing()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 99_999, Unit.G, Now);

        item.TryAdd(2, Unit.G).Should().BeFalse();

        (item.Amount, item.EnteredUnit).Should().Be(((decimal?)99_999m, (Unit?)Unit.G));
    }

    [Fact]
    public void A_row_does_not_mix_dimensions()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 50, Unit.G, Now);

        item.Invoking(i => i.TryAdd(1, Unit.Dl)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Updating_a_row_without_an_amount_keeps_it_and_with_one_replaces_it()
    {
        var item = ShoppingListItem.Create(HouseholdId, Tejfol.Id, 50, Unit.G, Now);

        item.Update(isChecked: true, null, null);
        (item.Checked, item.Amount, item.EnteredUnit).Should().Be((true, (decimal?)50m, (Unit?)Unit.G));

        item.Update(isChecked: false, 3, Unit.Dkg);
        (item.Checked, item.Amount, item.EnteredUnit).Should().Be((false, (decimal?)3m, (Unit?)Unit.Dkg));
    }

    [Fact]
    public void An_open_or_rejected_suggestion_and_an_unchecked_row_suppress_a_new_suggestion()
    {
        var open = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);
        var onList = ShoppingListItem.Create(HouseholdId, Tej.Id, 1, Unit.L, Now);
        var bought = ShoppingListItem.Create(HouseholdId, Tej.Id, 1, Unit.L, Now);
        bought.Update(isChecked: true, null, null);

        SuggestionRules.IsSuppressed(open, null).Should().BeTrue();
        SuggestionRules.IsSuppressed(null, onList).Should().BeTrue();
        SuggestionRules.IsSuppressed(null, bought).Should().BeFalse(); // bought: it may run out again
        SuggestionRules.IsSuppressed(null, null).Should().BeFalse();
    }

    [Fact]
    public void A_suggestion_made_by_a_cooking_remembers_it()
    {
        var cookingId = Guid.CreateVersion7();

        ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now, cookingId).CookingId.Should().Be(cookingId);
        ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now).CookingId.Should().BeNull();
    }

    [Fact]
    public void A_minimum_is_stored_in_base_units()
    {
        var minimum = IngredientMinimum.Create(HouseholdId, Tej, 5, Unit.Dl);

        (minimum.IngredientId, minimum.MinimumQuantity, minimum.EnteredUnit).Should().Be((Tej.Id, 500m, Unit.Dl));

        minimum.Change(1, Unit.L);

        (minimum.MinimumQuantity, minimum.EnteredUnit).Should().Be((1000m, Unit.L));
    }

    [Fact]
    public void A_minimum_is_positive_and_in_the_ingredients_dimension()
    {
        FluentActions.Invoking(() => IngredientMinimum.Create(HouseholdId, Tej, 0, Unit.Ml)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => IngredientMinimum.Create(HouseholdId, Tej, 500, Unit.G)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_cooking_line_remembers_that_it_made_a_suggestion()
    {
        var recipe = new RecipeCandidate(Guid.CreateVersion7(), "Tejes", 1,
            [new RecipeNeed(Tej.Id, Tej.Name, 200, Unit.Ml, false), new RecipeNeed(Tejfol.Id, Tejfol.Name, 100, Unit.G, false)]);
        var today = new DateOnly(2026, 10, 18);
        var milk = PantryItem.Create(HouseholdId, Tej, 200, Unit.Ml, Category.Dairy, today.AddDays(3), today, Now).Item;
        var sourCream = PantryItem.Create(HouseholdId, Tejfol, 300, Unit.G, Category.Dairy, today.AddDays(3), today, Now).Item;
        var (cooking, _) = Cooking.Create(HouseholdId, recipe, 1, Guid.CreateVersion7(),
            new Dictionary<Guid, decimal> { [Tej.Id] = 200, [Tejfol.Id] = 100 }, [milk, sourCream], Now);

        cooking.MarkSuggested(Tej.Id);

        cooking.Lines.Select(l => (l.IngredientId, l.Suggested)).Should().BeEquivalentTo([(Tej.Id, true), (Tejfol.Id, false)]);
        cooking.Invoking(c => c.MarkSuggested(Guid.CreateVersion7())).Should().Throw<ArgumentException>();
    }
}
