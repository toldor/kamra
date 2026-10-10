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

    // The amount in the unit the user entered, for display.
    public static decimal FromBase(decimal quantity, Unit unit) => throw new NotImplementedException();

    private static decimal Factor(Unit unit) => unit switch
    {
        Unit.G or Unit.Ml or Unit.Db => 1m,
        Unit.Dkg => 10m,
        Unit.Dl => 100m,
        Unit.Kg or Unit.L => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };
}
