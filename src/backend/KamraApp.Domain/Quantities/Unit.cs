namespace KamraApp.Domain.Quantities;

// ADR-0002: fixed, convertible units; no conversion between mass and volume.
public enum Unit
{
    G,
    Dkg,
    Kg,
    Ml,
    Dl,
    L,
    Db,
}

public enum Dimension
{
    Mass,
    Volume,
    Count,
}

// Quantities are stored and calculated in the base unit of their dimension (g, ml, db), so 30 dkg
// and 300 g are the same amount (data_model.md, Mennyiségek).
public static class Units
{
    public static Dimension DimensionOf(Unit unit) => unit switch
    {
        Unit.G or Unit.Dkg or Unit.Kg => Dimension.Mass,
        Unit.Ml or Unit.Dl or Unit.L => Dimension.Volume,
        Unit.Db => Dimension.Count,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };

    public static decimal ToBase(decimal amount, Unit unit) => amount * Factor(unit);

    // The unit quantities of the dimension are stored in (g, ml, db).
    public static Unit BaseUnit(Dimension dimension) => dimension switch
    {
        Dimension.Mass => Unit.G,
        Dimension.Volume => Unit.Ml,
        Dimension.Count => Unit.Db,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown dimension."),
    };

    // ADR-0002: an ingredient is measured only in the units of its dimension.
    public static void EnsureDimension(Unit unit, Dimension dimension)
    {
        if (DimensionOf(unit) != dimension)
        {
            throw new ArgumentException($"The unit {unit} does not measure {dimension} (ADR-0002).", nameof(unit));
        }
    }

    // The amount in the unit the user entered, for display.
    public static decimal FromBase(decimal quantity, Unit unit) => quantity / Factor(unit);

    private static decimal Factor(Unit unit) => unit switch
    {
        Unit.G or Unit.Ml or Unit.Db => 1m,
        Unit.Dkg => 10m,
        Unit.Dl => 100m,
        Unit.Kg or Unit.L => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };
}
