using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.ItemProposals;

// US-2 / ADR-0002: the unit word of the LLM ("kiló", "deka", "darab") to one of the seven fixed units.
// Null: not a convertible unit ("doboz", "csomag"), so the proposal has no unit until the user picks one.
public static class UnitWords
{
    private static readonly Dictionary<string, Unit> Known = new(StringComparer.Ordinal)
    {
        ["g"] = Unit.G,
        ["gr"] = Unit.G,
        ["gramm"] = Unit.G,
        ["dkg"] = Unit.Dkg,
        ["deka"] = Unit.Dkg,
        ["deko"] = Unit.Dkg,
        ["dekagramm"] = Unit.Dkg,
        ["kg"] = Unit.Kg,
        ["kilo"] = Unit.Kg,
        ["kilogramm"] = Unit.Kg,
        ["ml"] = Unit.Ml,
        ["milliliter"] = Unit.Ml,
        ["dl"] = Unit.Dl,
        ["deci"] = Unit.Dl,
        ["deciliter"] = Unit.Dl,
        ["l"] = Unit.L,
        ["liter"] = Unit.L,
        ["db"] = Unit.Db,
        ["darab"] = Unit.Db,
    };

    public static Unit? Map(string? word) => Known.TryGetValue(Words.Fold(word), out var unit) ? unit : null;
}
