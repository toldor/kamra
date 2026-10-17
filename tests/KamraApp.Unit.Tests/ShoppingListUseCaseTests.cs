using System.Globalization;
using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-5 in the Application: the request rules (every Domain rule is a 400 first, V-25) and the suggestion
// evaluator with an in-memory repository: what blocks a new suggestion (M15, data_model.md) and when a rejected
// one is cleared (M3). The API tests check the same through the database, with the lock (ADR-0008 6.).
public class ShoppingListUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient Tej = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);

    private readonly FakeShopping _shopping = new();

    [Theory]
    [InlineData("50", null, "unit")]       // an amount needs its unit
    [InlineData(null, "g", "amount")]      // and a unit its amount
    [InlineData("0", "g", "amount")]
    [InlineData("-1", "g", "amount")]
    [InlineData("1.0001", "g", "amount")]
    [InlineData("100001", "g", "amount")]
    [InlineData("50", "csomag", "unit")]
    public void A_list_amount_is_valid_only_with_its_unit_and_within_the_limits(string? amount, string? unit, string field)
    {
        var request = new AddShoppingListItemRequest { IngredientId = Tej.Id, Amount = Parse(amount), Unit = unit };

        var error = FluentActions.Invoking(() => RequestValidator.Validate(request)).Should().Throw<ValidationException>();

        error.Which.Errors.Should().ContainKey(field);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("50", "g")]
    [InlineData("0.5", "L")]
    public void A_row_may_have_no_amount_or_a_valid_one(string? amount, string? unit) =>
        FluentActions.Invoking(() => RequestValidator.Validate(new AddShoppingListItemRequest { IngredientId = Tej.Id, Amount = Parse(amount), Unit = unit }))
            .Should().NotThrow();

    [Fact]
    public void Adding_needs_an_ingredient_and_updating_needs_the_check()
    {
        FluentActions.Invoking(() => RequestValidator.Validate(new AddShoppingListItemRequest()))
            .Should().Throw<ValidationException>().Which.Errors.Should().ContainKey("ingredientId");
        FluentActions.Invoking(() => RequestValidator.Validate(new UpdateShoppingListItemRequest()))
            .Should().Throw<ValidationException>().Which.Errors.Should().ContainKey("checked");
    }

    [Fact]
    public void Accepting_and_setting_a_minimum_follow_the_same_amount_rules()
    {
        FluentActions.Invoking(() => RequestValidator.Validate(new AcceptShoppingSuggestionRequest { Amount = 100 }))
            .Should().Throw<ValidationException>().Which.Errors.Should().ContainKey("unit");
        FluentActions.Invoking(() => RequestValidator.Validate(new SetIngredientMinimumRequest { Amount = 0, Unit = "ml" }))
            .Should().Throw<ValidationException>().Which.Errors.Should().ContainKey("amount");
        FluentActions.Invoking(() => RequestValidator.Validate(new AcceptShoppingSuggestionRequest())).Should().NotThrow();
        FluentActions.Invoking(() => RequestValidator.Validate(new SetIngredientMinimumRequest())).Should().NotThrow();
    }

    [Fact]
    public async Task P14a_using_up_the_last_milk_makes_a_depleted_suggestion()
    {
        var made = await DecreaseAsync(1000, 0, MovementReason.Consumed);

        made.Should().BeTrue();
        _shopping.Suggestions.Should().ContainSingle()
            .Which.Should().Match<ShoppingSuggestion>(s => s.IngredientId == Tej.Id && s.Trigger == SuggestionTrigger.Depleted && s.State == SuggestionState.Open);
    }

    [Fact]
    public async Task P14b_throwing_away_the_last_milk_makes_no_suggestion()
    {
        (await DecreaseAsync(1000, 0, MovementReason.Discarded)).Should().BeFalse();

        _shopping.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task M15_no_suggestion_while_the_ingredient_is_on_the_list_unchecked()
    {
        _shopping.Items.Add(ShoppingListItem.Create(HouseholdId, Tej.Id, 1, Unit.L, Now));

        (await DecreaseAsync(1000, 0, MovementReason.Consumed)).Should().BeFalse();

        _shopping.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task M15_a_checked_row_does_not_stop_the_suggestion()
    {
        var bought = ShoppingListItem.Create(HouseholdId, Tej.Id, 1, Unit.L, Now);
        bought.Update(isChecked: true, 1, Unit.L);
        _shopping.Items.Add(bought);

        (await DecreaseAsync(1000, 0, MovementReason.Consumed)).Should().BeTrue();
    }

    [Fact]
    public async Task An_open_suggestion_blocks_a_second_one()
    {
        _shopping.Suggestions.Add(ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.BelowMinimum, Now));
        _shopping.Minimum = IngredientMinimum.Create(HouseholdId, Tej, 500, Unit.Ml);

        (await DecreaseAsync(300, 0, MovementReason.Consumed)).Should().BeFalse();

        _shopping.Suggestions.Should().ContainSingle();
    }

    [Fact]
    public async Task P15_a_rejected_suggestion_is_cleared_by_a_new_purchase_and_the_next_use_suggests_again()
    {
        var rejected = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.Depleted, Now);
        rejected.Reject(Now);
        _shopping.Suggestions.Add(rejected);

        (await ChangeAsync(0, 1000, MovementReason.Added)).Should().BeFalse(); // bought again: cleared
        rejected.ClearedAt.Should().Be(Now);
        (await DecreaseAsync(1000, 0, MovementReason.Consumed)).Should().BeTrue(); // used up again

        _shopping.Suggestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task P19_crossing_the_minimum_makes_a_below_minimum_suggestion()
    {
        _shopping.Minimum = IngredientMinimum.Create(HouseholdId, Tej, 500, Unit.Ml);

        (await DecreaseAsync(600, 400, MovementReason.Consumed)).Should().BeTrue();

        _shopping.Suggestions.Should().ContainSingle().Which.Trigger.Should().Be(SuggestionTrigger.BelowMinimum);
    }

    [Fact]
    public async Task P19c_setting_a_minimum_above_the_stock_suggests_at_once()
    {
        await new SuggestionEvaluator(_shopping).AfterMinimumChangeAsync(HouseholdId, Tej.Id, 300, 500, Now, TestContext.Current.CancellationToken);

        _shopping.Suggestions.Should().ContainSingle().Which.Trigger.Should().Be(SuggestionTrigger.BelowMinimum);
    }

    [Fact]
    public async Task M3_removing_the_minimum_clears_a_rejected_below_minimum_suggestion()
    {
        var rejected = ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.BelowMinimum, Now);
        rejected.Reject(Now);
        _shopping.Suggestions.Add(rejected);

        await new SuggestionEvaluator(_shopping).AfterMinimumChangeAsync(HouseholdId, Tej.Id, 300, null, Now, TestContext.Current.CancellationToken);

        rejected.ClearedAt.Should().Be(Now);
        _shopping.Suggestions.Should().ContainSingle();
    }

    private static decimal? Parse(string? amount) => amount is null ? null : decimal.Parse(amount, CultureInfo.InvariantCulture);

    private Task<bool> DecreaseAsync(decimal before, decimal after, MovementReason reason) => ChangeAsync(before, after, reason);

    private Task<bool> ChangeAsync(decimal before, decimal after, MovementReason reason) =>
        new SuggestionEvaluator(_shopping).AfterQuantityChangeAsync(HouseholdId, Tej.Id, before, after, reason, Now,
            TestContext.Current.CancellationToken);
}
