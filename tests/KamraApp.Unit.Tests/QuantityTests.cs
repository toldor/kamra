using System.Globalization;
using KamraApp.Domain.Quantities;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// ADR-0002: quantities are stored and compared in the base unit of their dimension (g, ml, db).
public class QuantityTests
{
    [Theory]
    [InlineData(Unit.G, "250", "250")]
    [InlineData(Unit.Dkg, "30", "300")]
    [InlineData(Unit.Kg, "1.5", "1500")]
    [InlineData(Unit.Ml, "30", "30")]
    [InlineData(Unit.Dl, "5", "500")]
    [InlineData(Unit.L, "1", "1000")]
    [InlineData(Unit.Db, "3", "3")]
    public void ToBase_converts_to_base_units(Unit unit, string amount, string expected)
    {
        Units.ToBase(decimal.Parse(amount, CultureInfo.InvariantCulture), unit)
            .Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(Unit.Dkg, "300", "30")]
    [InlineData(Unit.Kg, "1", "0.001")]
    [InlineData(Unit.Dl, "750", "7.5")]
    [InlineData(Unit.Db, "5", "5")]
    public void FromBase_converts_back_to_the_entered_unit(Unit unit, string quantity, string expected)
    {
        Units.FromBase(decimal.Parse(quantity, CultureInfo.InvariantCulture), unit)
            .Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(Unit.G, Dimension.Mass)]
    [InlineData(Unit.Dkg, Dimension.Mass)]
    [InlineData(Unit.Kg, Dimension.Mass)]
    [InlineData(Unit.Ml, Dimension.Volume)]
    [InlineData(Unit.Dl, Dimension.Volume)]
    [InlineData(Unit.L, Dimension.Volume)]
    [InlineData(Unit.Db, Dimension.Count)]
    public void Every_unit_belongs_to_exactly_one_dimension(Unit unit, Dimension dimension)
    {
        Units.DimensionOf(unit).Should().Be(dimension);
    }
}
