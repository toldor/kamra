using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.ItemProposals;

// US-2 / ADR-0002: the unit word of the LLM ("kiló", "deka", "darab") to one of the seven fixed units.
// Null: not a convertible unit ("doboz", "csomag"), so the proposal has no unit until the user picks one.
public static class UnitWords
{
    public static Unit? Map(string? word) => throw new NotImplementedException();
}
