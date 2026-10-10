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
    public static Dimension DimensionOf(Unit unit) => throw new NotImplementedException();

    public static decimal ToBase(decimal amount, Unit unit) => throw new NotImplementedException();
}
