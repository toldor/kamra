using KamraApp.Application.Common;
using KamraApp.Application.Cookings;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-4 use cases with in-memory ports, in the processing order of the S5 mini-spec (ADR-0008 2-3. pont):
// validation, repeated request, stock check, deduction, save; after a save failure the request id first.
// Amounts in requests and answers are in the recipe's unit (S5-D2).
public class CookingUseCaseTests
{
    // 18:00 in Budapest on 2026-10-11.
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly T = new(2026, 10, 11);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();

    private static readonly Ingredient Liszt = Ingredient.CreateSystem(Guid.CreateVersion7(), "finomliszt", "finomliszt", Dimension.Mass, Category.DryGoods);
    private static readonly Ingredient Tej = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);
    private static readonly Ingredient Tojas = Ingredient.CreateSystem(Guid.CreateVersion7(), "tojas", "tojás", Dimension.Count, Category.Eggs);
    private static readonly Ingredient Etolaj = Ingredient.CreateSystem(Guid.CreateVersion7(), "etolaj", "étolaj", Dimension.Volume, Category.Other);
    private static readonly Ingredient Viz = Ingredient.CreateSystem(Guid.CreateVersion7(), "viz", "víz", Dimension.Volume, Category.Beverages);
    private static readonly Ingredient So = Ingredient.CreateSystem(Guid.CreateVersion7(), "so", "só", Dimension.Mass, Category.DryGoods);

    // The approved seed recipe (recipes.json), 4 servings.
    private static readonly RecipeCandidate Palacsinta = new(Guid.CreateVersion7(), "Palacsinta", 4,
    [
        Need(Liszt, 200, Unit.G), Need(Tej, 5, Unit.Dl), Need(Tojas, 2, Unit.Db), Need(Etolaj, 30, Unit.Ml),
        Need(Viz, 1, Unit.Dl, staple: true), Need(So, 2, Unit.G, staple: true),
    ]);

    private readonly FakePantry _pantry = new();
    private readonly FakeCookings _cookings = new();

    private static RecipeNeed Need(Ingredient ingredient, decimal amount, Unit unit, bool staple = false) =>
        new(ingredient.Id, ingredient.Name, Units.ToBase(amount, unit), unit, staple);

    private PantryItem Stock(Ingredient ingredient, decimal amount, Unit unit, int daysToExpiry)
    {
        var item = PantryItem.Create(HouseholdId, ingredient, amount, unit, ingredient.DefaultCategory, T.AddDays(daysToExpiry), T, Now).Item;
        _pantry.Add(item, ingredient.Name);
        return item;
    }

    // P4 stock: enough for 6 servings.
    private void StockForSixServings()
    {
        Stock(Liszt, 1, Unit.Kg, 90);
        Stock(Tej, 1, Unit.L, 5);
        Stock(Tojas, 10, Unit.Db, 14);
        Stock(Etolaj, 1, Unit.L, 90);
    }

    private static ConfirmCookingRequest Request(Guid? requestId = null, int servings = 6, decimal liszt = 300, decimal tej = 7.5m,
        decimal tojas = 3, decimal etolaj = 45, Guid? recipeId = null) => new()
        {
            RequestId = requestId ?? Guid.CreateVersion7(),
            RecipeId = recipeId ?? Palacsinta.RecipeId,
            Servings = servings,
            Used =
            [
                new UsedAmount { IngredientId = Liszt.Id, Amount = liszt },
                new UsedAmount { IngredientId = Tej.Id, Amount = tej },
                new UsedAmount { IngredientId = Tojas.Id, Amount = tojas },
                new UsedAmount { IngredientId = Etolaj.Id, Amount = etolaj },
            ],
        };

    private Task<ConfirmCookingResult> ConfirmAsync(ConfirmCookingRequest? request) =>
        new ConfirmCooking(new FakeRecipes(), _pantry, _cookings, new FixedHousehold(), new FixedTime(Now))
            .ExecuteAsync(request, CancellationToken.None);

    private Task<CookingPreviewResponse> PreviewAsync(Guid recipeId, int? servings) =>
        new PreviewCooking(new FakeRecipes(), _pantry, new FixedHousehold(), new FixedTime(Now))
            .ExecuteAsync(recipeId, servings, CancellationToken.None);

    public static TheoryData<string, ConfirmCookingRequest> InvalidRequests() => new()
    {
        { "servings", Request(servings: 0) },
        { "servings", Request(servings: 101) },
        { "requestId", new ConfirmCookingRequest { RecipeId = Palacsinta.RecipeId, Servings = 4, Used = Request().Used } },
        { "recipeId", new ConfirmCookingRequest { RequestId = Guid.CreateVersion7(), Servings = 4, Used = Request().Used } },
        { "used", new ConfirmCookingRequest { RequestId = Guid.CreateVersion7(), RecipeId = Palacsinta.RecipeId, Servings = 4 } },
        { "used", Request(liszt: -1) },
        { "used", Request(liszt: 100_001) },
        { "used", Request(tej: 7.5001m) },
        { "used", WithUsed(new UsedAmount { IngredientId = Liszt.Id, Amount = 1 }) },
        { "used", WithUsed(new UsedAmount { Amount = 1 }) },
        { "used", WithUsed(new UsedAmount { IngredientId = So.Id }) },
    };

    // The full valid list plus one more entry.
    private static ConfirmCookingRequest WithUsed(UsedAmount extra)
    {
        var request = Request();
        return new ConfirmCookingRequest
        {
            RequestId = request.RequestId, RecipeId = request.RecipeId, Servings = request.Servings, Used = [.. request.Used!, extra],
        };
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_input_is_rejected_with_a_field_error_and_nothing_is_saved(string field, ConfirmCookingRequest request)
    {
        StockForSixServings();

        var error = await FluentActions.Awaiting(() => ConfirmAsync(request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey(field);
        _cookings.Saves.Should().Be(0);
    }

    [Fact]
    public async Task A_missing_body_is_a_validation_error()
    {
        await FluentActions.Awaiting(() => ConfirmAsync(null)).Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("staple")]
    [InlineData("unknown")]
    public async Task The_used_amounts_must_list_exactly_the_recipes_non_staple_ingredients(string variant)
    {
        StockForSixServings();
        var used = Request().Used!.ToList();
        switch (variant)
        {
            case "missing":
                used.RemoveAt(0);
                break;
            case "staple":
                used.Add(new UsedAmount { IngredientId = So.Id, Amount = 2 });
                break;
            default:
                used.Add(new UsedAmount { IngredientId = Guid.CreateVersion7(), Amount = 2 });
                break;
        }

        var error = await FluentActions.Awaiting(() => ConfirmAsync(new ConfirmCookingRequest
        {
            RequestId = Guid.CreateVersion7(), RecipeId = Palacsinta.RecipeId, Servings = 6, Used = used,
        })).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["used"].Should().Equal(CookingInput.UsedMessage);
        _cookings.Saves.Should().Be(0);
    }

    [Fact]
    public async Task An_unknown_or_foreign_recipe_is_not_found()
    {
        var error = await FluentActions.Awaiting(() => ConfirmAsync(Request(recipeId: Guid.CreateVersion7())))
            .Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("RECIPE_NOT_FOUND");
    }

    [Fact]
    public async Task P4_six_servings_of_pancakes_are_deducted_and_answered_in_the_recipe_units()
    {
        StockForSixServings();

        var result = await ConfirmAsync(Request());

        result.Created.Should().BeTrue();
        var cooking = result.Cooking;
        (cooking.RecipeId, cooking.RecipeName, cooking.Servings, cooking.CookedAt).Should().Be((Palacsinta.RecipeId, "Palacsinta", 6, Now));
        cooking.Lines.Select(l => (l.IngredientName, l.Unit, l.Required, l.Used, l.Shortage)).Should().Equal(
            ("étolaj", Unit.Ml, 45m, 45m, 0m),
            ("finomliszt", Unit.G, 300m, 300m, 0m),
            ("tej", Unit.Dl, 7.5m, 7.5m, 0m),
            ("tojás", Unit.Db, 3m, 3m, 0m));
        cooking.DepletedIngredients.Should().BeEmpty();
        _pantry.Items.Select(i => i.Quantity).Should().Equal(700m, 250m, 7m, 955m);
        _cookings.Saves.Should().Be(1);
    }

    [Fact]
    public async Task The_answer_names_the_ingredients_this_cooking_used_up()
    {
        Stock(Liszt, 300, Unit.G, 90);
        Stock(Tej, 1, Unit.L, 5);
        Stock(Tojas, 3, Unit.Db, 14);

        var result = await ConfirmAsync(Request(etolaj: 0));

        result.Cooking.DepletedIngredients.Should().Equal("finomliszt", "tojás");
        result.Cooking.Lines.Single(l => l.IngredientName == "étolaj").Shortage.Should().Be(45);
    }

    [Fact]
    public async Task P10_more_than_the_stock_is_refused_with_the_fresh_amounts_and_nothing_is_deducted()
    {
        Stock(Liszt, 200, Unit.G, 90);
        Stock(Tej, 5, Unit.Dl, 5);
        Stock(Tojas, 10, Unit.Db, 14);
        Stock(Etolaj, 1, Unit.L, 90);

        var error = await FluentActions.Awaiting(() => ConfirmAsync(Request())).Should().ThrowAsync<InsufficientStockException>();

        error.Which.Code.Should().Be("INSUFFICIENT_STOCK");
        error.Which.Available.Select(a => (a.IngredientName, a.Amount, a.Unit)).Should().Equal(
            ("étolaj", 1000m, Unit.Ml), ("finomliszt", 200m, Unit.G), ("tej", 5m, Unit.Dl), ("tojás", 10m, Unit.Db));
        _pantry.Items.Select(i => i.Quantity).Should().Equal(200m, 500m, 10m, 1000m);
        _cookings.Saves.Should().Be(0);
    }

    [Fact]
    public async Task P8_a_repeated_request_gets_the_stored_answer_without_a_second_deduction()
    {
        StockForSixServings();
        var request = Request();
        var first = await ConfirmAsync(request);

        var second = await ConfirmAsync(request);

        second.Created.Should().BeFalse();
        second.Cooking.Should().BeEquivalentTo(first.Cooking);
        _pantry.Items.Select(i => i.Quantity).Should().Equal(700m, 250m, 7m, 955m);
        _cookings.Saves.Should().Be(1);
    }

    [Theory]
    [InlineData("servings")]
    [InlineData("amount")]
    [InlineData("recipe")]
    public async Task The_same_request_id_with_other_content_is_an_idempotency_conflict(string change)
    {
        StockForSixServings();
        var requestId = Guid.CreateVersion7();
        await ConfirmAsync(Request(requestId));
        var other = change switch
        {
            "servings" => Request(requestId, servings: 5),
            "amount" => Request(requestId, liszt: 250),
            _ => Request(requestId, recipeId: Guid.CreateVersion7()),
        };

        var error = await FluentActions.Awaiting(() => ConfirmAsync(other)).Should().ThrowAsync<AppException>();

        error.Which.Code.Should().Be("IDEMPOTENCY_CONFLICT");
        _pantry.Items.Select(i => i.Quantity).Should().Equal(700m, 250m, 7m, 955m);
    }

    [Fact]
    public async Task When_the_save_loses_to_the_same_request_the_winners_answer_is_returned()
    {
        StockForSixServings();
        var request = Request();
        _cookings.BeforeSave = () => _cookings.SaveConcurrentDuplicate(Palacsinta, request);

        var result = await ConfirmAsync(request);

        result.Created.Should().BeFalse();
        result.Cooking.CookingId.Should().Be(_cookings.Saved.Single().Id);
    }

    [Fact]
    public async Task When_the_stock_changed_meanwhile_without_the_same_request_the_conflict_is_reported()
    {
        StockForSixServings();
        _cookings.BeforeSave = () => throw PantryErrors.ItemModified();

        var error = await FluentActions.Awaiting(() => ConfirmAsync(Request())).Should().ThrowAsync<ConflictException>();

        error.Which.Code.Should().Be("PANTRY_ITEM_MODIFIED");
    }

    [Fact]
    public async Task P4_the_preview_scales_to_the_servings_in_the_recipe_units_with_the_FEFO_split()
    {
        var expiredMilk = Stock(Tej, 2, Unit.Dl, -1);
        Stock(Tej, 1, Unit.L, 5);
        Stock(Liszt, 1, Unit.Kg, 90);

        var preview = await PreviewAsync(Palacsinta.RecipeId, 6);

        (preview.Name, preview.Servings, preview.RecipeServings, preview.Instructions).Should().Be(("Palacsinta", 6, 4, "Keverd ki."));
        preview.Staples.Should().Equal("só", "víz");
        preview.Lines.Select(l => (l.IngredientName, l.Unit, l.Required, l.Available, l.DefaultUsed, l.Shortage)).Should().Equal(
            ("étolaj", Unit.Ml, 45m, 0m, 0m, 45m),
            ("finomliszt", Unit.G, 300m, 1000m, 300m, 0m),
            ("tej", Unit.Dl, 7.5m, 12m, 7.5m, 0m),
            ("tojás", Unit.Db, 3m, 0m, 0m, 3m));
        preview.Lines.Single(l => l.IngredientName == "tej").Allocations.Should().Equal(
            new CookingAllocation(expiredMilk.ExpiryDate, 2m, Expired: true),
            new CookingAllocation(T.AddDays(5), 5.5m, Expired: false));
    }

    [Fact]
    public async Task The_preview_defaults_to_the_recipes_servings()
    {
        var preview = await PreviewAsync(Palacsinta.RecipeId, null);

        preview.Servings.Should().Be(4);
        preview.Lines.Single(l => l.IngredientName == "tej").Required.Should().Be(5m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task The_preview_rejects_servings_out_of_range(int servings)
    {
        var error = await FluentActions.Awaiting(() => PreviewAsync(Palacsinta.RecipeId, servings)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["servings"].Should().Equal(CookingInput.ServingsMessage);
    }

    [Fact]
    public async Task The_preview_of_an_unknown_recipe_is_not_found()
    {
        var error = await FluentActions.Awaiting(() => PreviewAsync(Guid.CreateVersion7(), 4)).Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("RECIPE_NOT_FOUND");
    }

    private sealed class FixedHousehold : ICurrentHousehold
    {
        public Guid HouseholdId => CookingUseCaseTests.HouseholdId;
    }

    private sealed class FakeRecipes : IRecipeRepository
    {
        public Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken) =>
            Task.FromResult(householdId == HouseholdId && recipeId == Palacsinta.RecipeId ? new RecipeDetails(Palacsinta, "Keverd ki.") : null);
    }

    private sealed class FakePantry : IPantryRepository
    {
        private readonly Dictionary<Guid, string> _names = [];

        public List<PantryItem> Items { get; } = [];

        public void Add(PantryItem item, string name)
        {
            Items.Add(item);
            _names[item.Id] = name;
        }

        public Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PantryEntry>>([.. Items.Where(i => i.HouseholdId == householdId && i.Quantity > 0)
                .Select(i => new PantryEntry(i, _names[i.Id]))]);

        public Task<IReadOnlyList<PantryItem>> ListForCookingAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PantryItem>>([.. Items.Where(i => i.HouseholdId == householdId && i.Quantity > 0
                && ingredientIds.Contains(i.IngredientId))]);

        public Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(PantryItem item, StockMovement movement) => throw new NotSupportedException();

        public void AddMovement(StockMovement movement) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeCookings : ICookingRepository
    {
        private Cooking? _pending;

        public List<Cooking> Saved { get; } = [];

        public int Saves { get; private set; }

        // Runs inside SaveChangesAsync, before the pending cooking is stored: simulates a concurrent request.
        public Action? BeforeSave { get; set; }

        public Task<Cooking?> FindByRequestAsync(Guid householdId, Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.SingleOrDefault(c => c.HouseholdId == householdId && c.RequestId == requestId));

        public void Add(Cooking cooking, IEnumerable<StockMovement> movements) => _pending = cooking;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            BeforeSave?.Invoke();
            Saved.Add(_pending!);
            Saves++;
            return Task.CompletedTask;
        }

        // Another request with the same id was saved first; this save hits the unique index.
        public void SaveConcurrentDuplicate(RecipeCandidate recipe, ConfirmCookingRequest request)
        {
            var used = request.Used!.ToDictionary(u => u.IngredientId!.Value,
                u => Units.ToBase(u.Amount!.Value, recipe.Needs.Single(n => n.IngredientId == u.IngredientId).Unit));
            Saved.Add(Cooking.Create(HouseholdId, recipe, request.Servings!.Value, request.RequestId!.Value, used, Plenty(), Now).Cooking);
            throw new DuplicateCookingRequestException(new InvalidOperationException("23505"));
        }

        // The other request's own stock (the real database rolled this request's changes back).
        private static List<PantryItem> Plenty() =>
        [
            .. new[] { (Liszt, Unit.Kg), (Tej, Unit.L), (Tojas, Unit.Db), (Etolaj, Unit.L) }
                .Select(s => PantryItem.Create(HouseholdId, s.Item1, 100, s.Item2, s.Item1.DefaultCategory, T, T, Now).Item),
        ];
    }
}
