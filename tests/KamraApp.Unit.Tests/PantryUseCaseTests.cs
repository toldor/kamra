using System.Globalization;
using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-1 use cases with in-memory ports. Every Domain rule must be rejected here as VALIDATION_FAILED
// (or a not-found code) before the Domain is called, so invalid input never becomes a 500 (V-25).
public class PantryUseCaseTests
{
    // 10:00 in Budapest on 2026-10-10.
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient SourCream =
        Ingredient.CreateSystem(Guid.CreateVersion7(), "tejfol", "tejföl", Dimension.Mass, Category.Dairy);

    private readonly FakePantry _pantry = new();

    [Theory]
    [InlineData("0", "g", null, "amount")]
    [InlineData("-1", "g", null, "amount")]
    [InlineData("1.0001", "g", null, "amount")]
    [InlineData("100001", "g", null, "amount")]
    [InlineData("200", "csomag", null, "unit")]
    [InlineData("200", "1", null, "unit")]
    [InlineData("200", "g", "tejtermek", "category")]
    public async Task Add_rejects_invalid_input_with_a_field_error(string amount, string unit, string? category, string field)
    {
        var request = new AddPantryItemRequest
        {
            IngredientId = SourCream.Id,
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            Unit = unit,
            Category = category,
        };

        var error = await FluentActions.Awaiting(() => AddAsync(request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey(field);
        _pantry.Saves.Should().Be(0);
    }

    [Fact]
    public async Task Add_rejects_missing_required_fields()
    {
        var error = await FluentActions.Awaiting(() => AddAsync(new AddPantryItemRequest())).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Keys.Should().Contain(["ingredientId", "amount", "unit"]);
    }

    [Fact]
    public async Task Add_rejects_a_unit_from_another_dimension()
    {
        var request = new AddPantryItemRequest { IngredientId = SourCream.Id, Amount = 2, Unit = "dl" };

        var error = await FluentActions.Awaiting(() => AddAsync(request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["unit"].Should().Equal("A(z) tejföl tömegben mérhető: g, dkg vagy kg.");
    }

    [Fact]
    public async Task Add_requires_an_expiry_date_for_Other()
    {
        var request = new AddPantryItemRequest { IngredientId = SourCream.Id, Amount = 200, Unit = "g", Category = "other" };

        var error = await FluentActions.Awaiting(() => AddAsync(request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey("expiryDate");
    }

    [Fact]
    public async Task Add_rejects_an_ingredient_the_household_cannot_see()
    {
        var request = new AddPantryItemRequest { IngredientId = Guid.CreateVersion7(), Amount = 200, Unit = "g" };

        var error = await FluentActions.Awaiting(() => AddAsync(request)).Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("INGREDIENT_NOT_FOUND");
        _pantry.Saves.Should().Be(0);
    }

    [Fact]
    public async Task Add_defaults_the_category_and_estimates_expiry_from_today_in_Budapest()
    {
        var lateEvening = new DateTimeOffset(2026, 10, 9, 23, 30, 0, TimeSpan.Zero); // already 10 October in Budapest
        var request = new AddPantryItemRequest { IngredientId = SourCream.Id, Amount = 20, Unit = "dkg" };

        var item = await AddAsync(request, lateEvening);

        item.IngredientName.Should().Be("tejföl");
        item.Amount.Should().Be(20m);
        item.Unit.Should().Be(Unit.Dkg);
        item.Quantity.Should().Be(200m);
        item.Category.Should().Be(Category.Dairy);
        item.ExpiryDate.Should().Be(new DateOnly(2026, 10, 17));
        item.ExpiryEstimated.Should().BeTrue();
        item.SoonExpiring.Should().BeFalse();
        _pantry.Movements.Should().ContainSingle(m => m.Reason == MovementReason.Added && m.Delta == 200m);
        _pantry.Saves.Should().Be(1);
    }

    [Theory]
    [InlineData("-1", "consumed", "dairy", "amount")]
    [InlineData("100", "elfogyott", "dairy", "reason")]
    [InlineData("100", "consumed", null, "category")]
    public async Task Update_rejects_invalid_input_with_a_field_error(string amount, string reason, string? category, string field)
    {
        var item = StockItem();
        var request = new UpdatePantryItemRequest
        {
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            Unit = "g",
            Category = category,
            Reason = reason,
            Version = item.Version,
        };

        var error = await FluentActions.Awaiting(() => UpdateAsync(item.Id, request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey(field);
    }

    [Fact]
    public async Task Update_rejects_a_decrease_without_reason_and_changes_nothing()
    {
        var item = StockItem();

        var error = await FluentActions.Awaiting(() => UpdateAsync(item.Id, Edit(item, amount: 150))).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["reason"].Should().Equal(PantryInput.DecreaseNeedsReasonMessage);
        item.Quantity.Should().Be(200m);
        _pantry.Saves.Should().Be(0);
    }

    [Fact]
    public async Task Update_rejects_an_increase_with_a_decrease_reason()
    {
        var item = StockItem();

        var error = await FluentActions.Awaiting(() => UpdateAsync(item.Id, Edit(item, amount: 250, reason: "consumed"))).Should().ThrowAsync<ValidationException>();

        error.Which.Errors["reason"].Should().Equal(PantryInput.IncreaseReasonMessage);
    }

    [Fact]
    public async Task Update_rejects_a_unit_from_another_dimension()
    {
        var item = StockItem();

        var error = await FluentActions.Awaiting(() => UpdateAsync(item.Id, Edit(item, amount: 1, unit: "l", reason: "corrected"))).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey("unit");
    }

    [Fact]
    public async Task Update_of_a_missing_item_is_not_found()
    {
        var item = StockItem();

        var error = await FluentActions.Awaiting(() => UpdateAsync(Guid.CreateVersion7(), Edit(item, amount: 100, reason: "consumed"))).Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("PANTRY_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task Update_decreases_with_a_reason_and_records_the_movement()
    {
        var item = StockItem();

        var updated = await UpdateAsync(item.Id, Edit(item, amount: 15, unit: "dkg", reason: "discarded"));

        updated.Quantity.Should().Be(150m);
        updated.Amount.Should().Be(15m);
        _pantry.Movements.Should().ContainSingle(m => m.Reason == MovementReason.Discarded && m.Delta == -50m);
        _pantry.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Update_re_estimates_a_missing_expiry_from_the_entry_day()
    {
        var item = StockItem();

        var updated = await UpdateAsync(item.Id, Edit(item, amount: 200, category: "frozen"), Now.AddDays(5));

        updated.ExpiryDate.Should().Be(new DateOnly(2026, 10, 10).AddDays(90), "the estimate starts from the entry day, not the edit");
        updated.ExpiryEstimated.Should().BeTrue();
    }

    [Fact]
    public async Task List_rejects_an_unknown_category_filter()
    {
        var list = () => new ListPantryItems(_pantry, new FixedHousehold(HouseholdId), new FixedTime(Now))
            .ExecuteAsync(search: null, category: "tejtermek", expiringSoon: null, CancellationToken.None);

        (await list.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("category");
    }

    private Task<PantryItemResponse> AddAsync(AddPantryItemRequest request, DateTimeOffset? now = null) =>
        new AddPantryItem(_pantry, new FakeIngredients(SourCream), new FixedHousehold(HouseholdId), new FixedTime(now ?? Now))
            .ExecuteAsync(request, CancellationToken.None);

    private Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, DateTimeOffset? now = null) =>
        new UpdatePantryItem(_pantry, new FixedHousehold(HouseholdId), new FixedTime(now ?? Now))
            .ExecuteAsync(id, request, CancellationToken.None);

    // 200 g sour cream entered on 2026-10-10 with an estimated expiry.
    private PantryItem StockItem()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, new DateOnly(2026, 10, 10), Now);
        _pantry.Items.Add(item);
        return item;
    }

    private static UpdatePantryItemRequest Edit(PantryItem item, decimal amount, string unit = "g", string category = "dairy", string? reason = null) =>
        new() { Amount = amount, Unit = unit, Category = category, Reason = reason, Version = item.Version };

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

    private sealed class FakePantry : IPantryRepository
    {
        public List<PantryItem> Items { get; } = [];

        public List<StockMovement> Movements { get; } = [];

        public int Saves { get; private set; }

        public Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PantryEntry>>([.. Items.Select(i => new PantryEntry(i, SourCream.Name))]);

        public Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id && i.HouseholdId == householdId && i.Quantity > 0) is { } item
                ? new PantryEntry(item, SourCream.Name)
                : null);

        public void Add(PantryItem item, StockMovement movement)
        {
            Items.Add(item);
            Movements.Add(movement);
        }

        public void AddMovement(StockMovement movement) => Movements.Add(movement);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }
}
