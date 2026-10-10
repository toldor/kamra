using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-1 and data_model.md: every quantity change of a pantry item produces a stock movement, so the
// item's quantity always equals the sum of its movements (CAP-08).
public class PantryItemTests
{
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient SourCream =
        Ingredient.CreateSystem(Guid.CreateVersion7(), "tejfol", "tejföl", Dimension.Mass, Category.Dairy);

    [Fact]
    public void Create_stores_base_units_estimates_expiry_and_records_an_Added_movement()
    {
        var (item, movement) = PantryItem.Create(HouseholdId, SourCream, 20m, Unit.Dkg, Category.Dairy, expiryDate: null, Day, Now);

        item.Id.Version.Should().Be(7);
        item.HouseholdId.Should().Be(HouseholdId);
        item.IngredientId.Should().Be(SourCream.Id);
        item.Quantity.Should().Be(200m);
        item.EnteredUnit.Should().Be(Unit.Dkg);
        item.ExpiryDate.Should().Be(Day.AddDays(7));
        item.ExpiryEstimated.Should().BeTrue();
        movement.PantryItemId.Should().Be(item.Id);
        movement.HouseholdId.Should().Be(HouseholdId);
        movement.Delta.Should().Be(200m);
        movement.Reason.Should().Be(MovementReason.Added);
        movement.ExpiryDateAtMovement.Should().Be(item.ExpiryDate);
        movement.OccurredAt.Should().Be(Now);
    }

    [Fact]
    public void A_given_expiry_date_is_not_estimated()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, Day.AddDays(3), Day, Now);

        item.ExpiryDate.Should().Be(Day.AddDays(3));
        item.ExpiryEstimated.Should().BeFalse();
    }

    [Fact]
    public void Other_category_requires_an_expiry_date()
    {
        var create = () => PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Other, expiryDate: null, Day, Now);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_unit_from_another_dimension_is_rejected()
    {
        var create = () => PantryItem.Create(HouseholdId, SourCream, 200m, Unit.Ml, Category.Dairy, expiryDate: null, Day, Now);

        create.Should().Throw<ArgumentException>("ADR-0002: no conversion between mass and volume");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Zero_or_negative_amount_is_rejected_on_create(int amount)
    {
        var create = () => PantryItem.Create(HouseholdId, SourCream, amount, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void More_than_three_decimals_in_base_units_is_rejected()
    {
        // numeric(12,3): PostgreSQL would round the value, and the stored log would drift from memory.
        var create = () => PantryItem.Create(HouseholdId, SourCream, 1.2345m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);
        var change = () => item.ChangeQuantity(0.0000001m, Unit.Kg, MovementReason.Consumed, Now);

        create.Should().Throw<ArgumentException>();
        change.Should().Throw<ArgumentException>();
        item.Quantity.Should().Be(200m);
    }

    [Fact]
    public void A_quantity_above_the_column_capacity_is_rejected()
    {
        var create = () => PantryItem.Create(HouseholdId, SourCream, 1_000_000m, Unit.Kg, Category.Dairy, expiryDate: null, Day, Now);
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);
        var change = () => item.ChangeQuantity(1_000_000m, Unit.Kg, reason: null, Now);

        create.Should().Throw<ArgumentException>();
        change.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_decrease_without_reason_is_rejected_and_changes_nothing()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        var decrease = () => item.ChangeQuantity(150m, Unit.G, reason: null, Now);

        decrease.Should().Throw<ArgumentException>();
        item.Quantity.Should().Be(200m);
    }

    [Fact]
    public void A_decrease_records_the_given_reason()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        var movement = item.ChangeQuantity(150m, Unit.G, MovementReason.Discarded, Now);

        item.Quantity.Should().Be(150m);
        movement!.Delta.Should().Be(-50m);
        movement.Reason.Should().Be(MovementReason.Discarded);
    }

    [Fact]
    public void An_increase_is_recorded_as_Corrected()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        var movement = item.ChangeQuantity(25m, Unit.Dkg, reason: null, Now);

        item.Quantity.Should().Be(250m);
        item.EnteredUnit.Should().Be(Unit.Dkg);
        movement!.Delta.Should().Be(50m);
        movement.Reason.Should().Be(MovementReason.Corrected, "a new purchase is a new pantry item, so an increase is a correction");
    }

    [Theory]
    [InlineData(MovementReason.Added)]
    [InlineData(MovementReason.Consumed)]
    [InlineData(MovementReason.Discarded)]
    public void An_increase_with_a_decrease_reason_is_rejected(MovementReason reason)
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        var increase = () => item.ChangeQuantity(250m, Unit.G, reason, Now);

        increase.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Quantity_cannot_go_below_zero()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        var decrease = () => item.ChangeQuantity(-1m, Unit.G, MovementReason.Consumed, Now);

        decrease.Should().Throw<ArgumentException>();
        item.Quantity.Should().Be(200m);
    }

    [Fact]
    public void An_unchanged_quantity_records_no_movement()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        item.ChangeQuantity(20m, Unit.Dkg, reason: null, Now).Should().BeNull();
        item.EnteredUnit.Should().Be(Unit.Dkg);
    }

    [Fact]
    public void Quantity_equals_the_sum_of_movements()
    {
        var (item, added) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);
        var movements = new List<StockMovement?>
        {
            added,
            item.ChangeQuantity(120m, Unit.G, MovementReason.Consumed, Now),
            item.ChangeQuantity(15m, Unit.Dkg, reason: null, Now),
            item.ChangeQuantity(0m, Unit.G, MovementReason.Discarded, Now),
        };

        item.Quantity.Should().Be(0m);
        movements.Sum(m => m!.Delta).Should().Be(item.Quantity);
    }

    [Fact]
    public void Changing_details_re_estimates_a_missing_expiry_from_the_entry_day()
    {
        var (item, _) = PantryItem.Create(HouseholdId, SourCream, 200m, Unit.G, Category.Dairy, expiryDate: null, Day, Now);

        // data_model.md: the estimate is the entry day + the category's days, also when edited days later.
        item.UpdateDetails(Category.Frozen, expiryDate: null, entryDay: Day, Now.AddDays(5));

        item.Category.Should().Be(Category.Frozen);
        item.ExpiryDate.Should().Be(Day.AddDays(90));
        item.ExpiryEstimated.Should().BeTrue();
    }
}
