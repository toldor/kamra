using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-5 use cases with in-memory ports: what they save, lock and answer, and their error paths. Written after the
// implementation, when the Application coverage gate showed that only the API tests ran them (P-16); every test was
// checked against a broken use case (mutation). The rules themselves are in ShoppingListTests and
// ShoppingListUseCaseTests, the database and the lock in ShoppingListApiTests.
public class ShoppingListCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient Tej = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);
    private static readonly Ingredient Tejfol = Ingredient.CreateSystem(Guid.CreateVersion7(), "tejfol", "tejföl", Dimension.Mass, Category.Dairy);
    private static readonly Ingredient Oregano = Ingredient.CreateSystem(Guid.CreateVersion7(), "oregano", "őrölt oregánó", Dimension.Mass, Category.DryGoods);
    private static readonly Ingredient Olaj = Ingredient.CreateSystem(Guid.CreateVersion7(), "olaj", "olaj", Dimension.Volume, Category.Other);

    private readonly FakeShopping _shopping = new();
    private readonly NoIngredientLocks _locks = new();
    private readonly FakeIngredients _ingredients = new(Tej, Tejfol, Oregano, Olaj);
    private readonly FakePantryTotals _pantry = new();

    public ShoppingListCommandTests()
    {
        foreach (var ingredient in new[] { Tej, Tejfol, Oregano, Olaj })
        {
            _shopping.Ingredients[ingredient.Id] = (ingredient.Name, ingredient.Dimension);
        }
    }

    [Fact]
    public async Task Adding_a_new_ingredient_makes_a_row_inside_the_lock()
    {
        var row = await AddAsync(Tejfol, 50, "g");

        (row.IngredientName, row.Amount, row.Checked).Should().Be(("tejföl", new AmountResponse(50, Unit.G), false));
        _shopping.Items.Should().ContainSingle();
        _locks.Locked.Should().Equal(Tejfol.Id);
        _shopping.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Adding_the_same_ingredient_again_adds_up_on_the_row()
    {
        await AddAsync(Tejfol, 50, "g");

        var row = await AddAsync(Tejfol, 100, "g");

        row.Amount.Should().Be(new AmountResponse(150, Unit.G));
        _shopping.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task A_sum_over_the_limit_is_a_field_error_and_nothing_is_saved()
    {
        await AddAsync(Tejfol, 99_999, "g");

        var error = await FluentActions.Awaiting(() => AddAsync(Tejfol, 2, "g")).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["amount"].Should().Equal(ShoppingListInput.LimitMessage);
        _shopping.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Adding_needs_an_ingredient_the_household_can_see_and_a_unit_of_its_dimension()
    {
        var unknown = new AddShoppingListItemRequest { IngredientId = Guid.CreateVersion7(), Amount = 1, Unit = "g" };
        (await FluentActions.Awaiting(() => Add().ExecuteAsync(unknown, CancellationToken.None)).Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("INGREDIENT_NOT_FOUND");

        (await FluentActions.Awaiting(() => AddAsync(Tejfol, 2, "dl")).Should().ThrowAsync<ValidationException>())
            .Which.Errors["unit"].Should().Equal("A(z) tejföl tömegben mérhető: g, dkg vagy kg.");
        _shopping.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task The_list_is_in_Hungarian_order_and_a_row_may_have_no_amount()
    {
        await AddAsync(Oregano, 10, "g");
        await AddAsync(Olaj, null, null);

        var list = await new ListShoppingList(_shopping, new FixedHousehold(HouseholdId)).ExecuteAsync(CancellationToken.None);

        list.Select(r => (r.IngredientName, r.Amount)).Should().Equal(("olaj", null), ("őrölt oregánó", new AmountResponse(10, Unit.G)));
    }

    [Fact]
    public async Task Updating_a_row_checks_it_and_sets_its_amount()
    {
        var row = await AddAsync(Tej, 1, "l");

        var updated = await Update().ExecuteAsync(row.Id, new UpdateShoppingListItemRequest { Checked = true, Amount = 5, Unit = "dl" },
            CancellationToken.None);

        (updated.Checked, updated.Amount).Should().Be((true, new AmountResponse(5, Unit.Dl)));
        _shopping.Saves.Should().Be(2);
    }

    [Fact]
    public async Task Updating_rejects_a_missing_row_and_a_unit_of_another_dimension()
    {
        var row = await AddAsync(Tej, 1, "l");
        var missing = Guid.CreateVersion7();

        (await FluentActions.Awaiting(() => Update().ExecuteAsync(missing, new UpdateShoppingListItemRequest { Checked = true }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("SHOPPING_LIST_ITEM_NOT_FOUND");
        (await FluentActions.Awaiting(() => Update().ExecuteAsync(row.Id, new UpdateShoppingListItemRequest { Checked = true, Amount = 1, Unit = "g" },
            CancellationToken.None)).Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("unit");
    }

    [Fact]
    public async Task Another_households_row_is_missing()
    {
        var row = await AddAsync(Tej, 1, "l");
        var other = new UpdateShoppingListItem(_shopping, _ingredients, _locks, new FixedHousehold(Guid.CreateVersion7()));

        (await FluentActions.Awaiting(() => other.ExecuteAsync(row.Id, new UpdateShoppingListItemRequest { Checked = true }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("SHOPPING_LIST_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task Deleting_removes_the_row_once()
    {
        var row = await AddAsync(Tej, 1, "l");
        var delete = new DeleteShoppingListItem(_shopping, _locks, new FixedHousehold(HouseholdId));

        await delete.ExecuteAsync(row.Id, CancellationToken.None);

        _shopping.Items.Should().BeEmpty();
        (await FluentActions.Awaiting(() => delete.ExecuteAsync(row.Id, CancellationToken.None)).Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("SHOPPING_LIST_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task M12_M13_a_below_minimum_suggestion_tells_why_and_how_much_in_the_minimums_unit()
    {
        _shopping.Minimum = IngredientMinimum.Create(HouseholdId, Tej, 5, Unit.Dl);
        _shopping.Totals[Tej.Id] = 400;
        var cookingId = Guid.CreateVersion7();
        _shopping.CookingRecipes[cookingId] = "Túrós csusza";
        _shopping.Suggestions.Add(ShoppingSuggestion.Create(HouseholdId, Tejfol.Id, SuggestionTrigger.Depleted, Now.AddMinutes(1), cookingId));
        _shopping.Suggestions.Add(ShoppingSuggestion.Create(HouseholdId, Tej.Id, SuggestionTrigger.BelowMinimum, Now));

        var suggestions = await new ListShoppingSuggestions(_shopping, new FixedHousehold(HouseholdId)).ExecuteAsync(CancellationToken.None);

        suggestions.Select(s => (s.IngredientName, s.Trigger, s.RecipeName, s.CurrentAmount, s.MinimumAmount, s.SuggestedAmount)).Should().Equal(
            ("tej", SuggestionTrigger.BelowMinimum, null, new AmountResponse(4, Unit.Dl), new AmountResponse(5, Unit.Dl), new AmountResponse(1, Unit.Dl)),
            ("tejföl", SuggestionTrigger.Depleted, "Túrós csusza", null, null, null));
    }

    [Fact]
    public async Task Accepting_puts_the_suggestion_on_the_list_with_the_given_amount()
    {
        var suggestion = Suggest(Tej);

        await AcceptAsync(suggestion, 500, "ml");

        Row(Tej).Should().Be(new AmountResponse(500, Unit.Ml));
        suggestion.State.Should().Be(SuggestionState.Accepted);
        _locks.Locked.Should().Equal(Tej.Id);
        _shopping.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Accepting_adds_to_a_row_that_is_already_there()
    {
        await AddAsync(Tej, 1, "l");

        await AcceptAsync(Suggest(Tej), 500, "ml");

        Row(Tej).Should().Be(new AmountResponse(1500, Unit.Ml));
    }

    [Fact]
    public async Task Accepting_again_adds_nothing_and_only_ends_the_transaction()
    {
        var suggestion = Suggest(Tej);
        await AcceptAsync(suggestion, 500, "ml");

        await AcceptAsync(suggestion, 500, "ml");

        Row(Tej).Should().Be(new AmountResponse(500, Unit.Ml));
        (suggestion.State, _shopping.Saves).Should().Be((SuggestionState.Accepted, 2));
    }

    [Fact]
    public async Task Accepting_a_rejected_suggestion_is_a_conflict()
    {
        var suggestion = Suggest(Tej);
        suggestion.Reject(Now);

        (await FluentActions.Awaiting(() => AcceptAsync(suggestion, null, null)).Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");
        _shopping.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Accepting_fails_for_a_missing_suggestion_a_wrong_unit_or_a_sum_over_the_limit()
    {
        var accept = Accept();
        (await FluentActions.Awaiting(() => accept.ExecuteAsync(Guid.CreateVersion7(), new AcceptShoppingSuggestionRequest(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("SHOPPING_SUGGESTION_NOT_FOUND");

        var suggestion = Suggest(Tejfol);
        (await FluentActions.Awaiting(() => AcceptAsync(suggestion, 1, "l")).Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("unit");

        await AddAsync(Tejfol, 99_999, "g");
        (await FluentActions.Awaiting(() => AcceptAsync(suggestion, 2, "g")).Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("amount");
    }

    [Fact]
    public async Task Rejecting_twice_keeps_it_rejected_and_rejecting_an_accepted_one_is_a_conflict()
    {
        var reject = new RejectShoppingSuggestion(_shopping, _locks, new FixedHousehold(HouseholdId), new FixedTime(Now));
        var rejected = Suggest(Tej);
        var accepted = Suggest(Tejfol);
        accepted.Accept(Now);

        await reject.ExecuteAsync(rejected.Id, CancellationToken.None);
        await reject.ExecuteAsync(rejected.Id, CancellationToken.None);

        (rejected.State, rejected.ResolvedAt, _shopping.Saves).Should().Be((SuggestionState.Rejected, (DateTimeOffset?)Now, 2));
        (await FluentActions.Awaiting(() => reject.ExecuteAsync(accepted.Id, CancellationToken.None)).Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");
        (await FluentActions.Awaiting(() => reject.ExecuteAsync(Guid.CreateVersion7(), CancellationToken.None)).Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("SHOPPING_SUGGESTION_NOT_FOUND");
    }

    [Fact]
    public async Task P19c_setting_a_minimum_stores_it_and_suggests_below_it()
    {
        _pantry.Totals[Tej.Id] = 300;

        await SetMinimumAsync(Tej, 500, "ml");

        (_shopping.Minimum!.MinimumQuantity, _shopping.Minimum.EnteredUnit).Should().Be((500m, Unit.Ml));
        _shopping.Suggestions.Should().ContainSingle().Which.Trigger.Should().Be(SuggestionTrigger.BelowMinimum);
        _locks.Locked.Should().Equal(Tej.Id);
        _shopping.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Changing_a_minimum_keeps_one_row_and_removing_it_clears_a_rejected_suggestion()
    {
        _pantry.Totals[Tej.Id] = 300;
        await SetMinimumAsync(Tej, 200, "ml");
        await SetMinimumAsync(Tej, 1, "l");
        _shopping.Minimums.Should().ContainSingle().Which.MinimumQuantity.Should().Be(1000);
        var suggestion = _shopping.Suggestions.Single();
        suggestion.Reject(Now);

        await SetMinimumAsync(Tej, null, null);

        _shopping.Minimums.Should().BeEmpty();
        suggestion.ClearedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Setting_a_minimum_needs_a_visible_ingredient_and_a_unit_of_its_dimension()
    {
        var unknown = Ingredient.CreateSystem(Guid.CreateVersion7(), "x", "x", Dimension.Mass, Category.Other);

        (await FluentActions.Awaiting(() => SetMinimumAsync(unknown, 1, "g")).Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("INGREDIENT_NOT_FOUND");
        (await FluentActions.Awaiting(() => SetMinimumAsync(Tej, 1, "g")).Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("unit");
        _shopping.Minimums.Should().BeEmpty();
    }

    [Fact]
    public async Task The_ingredient_list_shows_the_households_minimum()
    {
        _shopping.Minimum = IngredientMinimum.Create(HouseholdId, Tej, 5, Unit.Dl);

        var list = await new ListIngredients(_ingredients, _shopping, new FixedHousehold(HouseholdId))
            .ExecuteAsync(new ListIngredientsQuery(), CancellationToken.None);

        list.Single(i => i.Name == "tej").Minimum.Should().Be(new AmountResponse(5, Unit.Dl));
        list.Single(i => i.Name == "tejföl").Minimum.Should().BeNull();
    }

    private ShoppingSuggestion Suggest(Ingredient ingredient)
    {
        var suggestion = ShoppingSuggestion.Create(HouseholdId, ingredient.Id, SuggestionTrigger.Depleted, Now);
        _shopping.Suggestions.Add(suggestion);
        return suggestion;
    }

    private AddShoppingListItem Add() => new(_shopping, _ingredients, _locks, new FixedHousehold(HouseholdId), new FixedTime(Now));

    private UpdateShoppingListItem Update() => new(_shopping, _ingredients, _locks, new FixedHousehold(HouseholdId));

    private AcceptShoppingSuggestion Accept() => new(_shopping, _ingredients, _locks, new FixedHousehold(HouseholdId), new FixedTime(Now));

    private Task<ShoppingListItemResponse> AddAsync(Ingredient ingredient, decimal? amount, string? unit) =>
        Add().ExecuteAsync(new AddShoppingListItemRequest { IngredientId = ingredient.Id, Amount = amount, Unit = unit }, CancellationToken.None);

    private AmountResponse? Row(Ingredient ingredient)
    {
        var item = _shopping.Items.Should().ContainSingle(i => i.IngredientId == ingredient.Id).Which;
        return AmountResponse.Of(item.Quantity, item.EnteredUnit);
    }

    private Task AcceptAsync(ShoppingSuggestion suggestion, decimal? amount, string? unit) =>
        Accept().ExecuteAsync(suggestion.Id, new AcceptShoppingSuggestionRequest { Amount = amount, Unit = unit }, CancellationToken.None);

    private Task SetMinimumAsync(Ingredient ingredient, decimal? amount, string? unit) =>
        new SetIngredientMinimum(_ingredients, _shopping, _pantry, _locks, new SuggestionEvaluator(_shopping), new FixedHousehold(HouseholdId),
                new FixedTime(Now))
            .ExecuteAsync(ingredient.Id, new SetIngredientMinimumRequest { Amount = amount, Unit = unit }, CancellationToken.None);

    private sealed class FixedHousehold(Guid id) : ICurrentHousehold
    {
        public Guid HouseholdId => id;
    }

    private sealed class FakeIngredients(params Ingredient[] visible) : IIngredientRepository
    {
        public Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(visible);

        public Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(visible.FirstOrDefault(i => i.Id == id));
    }

    // Only the total matters for the minimum (US-5).
    private sealed class FakePantryTotals : IPantryRepository
    {
        public Dictionary<Guid, decimal> Totals { get; } = [];

        public Task<decimal> TotalAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
            Task.FromResult(Totals.GetValueOrDefault(ingredientId));

        public Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PantryItem>> ListForCookingAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Add(PantryItem item, StockMovement movement) => throw new NotSupportedException();

        public void AddMovement(StockMovement movement) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
