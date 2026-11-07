namespace KamraApp.Application.ItemProposals;

// One item as the LLM extracted it (ADR-0009): only words, nothing decided yet. The ingredient is a name from
// the list the parser was given, or null ("-": not on the list); the unit and the expiry are as the user wrote them.
public sealed record ParsedItem(string Text, string? IngredientName, decimal? Amount, string? UnitText, string? ExpiryText);

// AGENTS.md 5.: the LLM sits behind this port, so tests never call it. Enabled is the Llm:Enabled switch
// (us2_minispec K10): when off, the Infrastructure registers an implementation that is never called.
// ParseAsync throws LLM_UNAVAILABLE (timeout or outage after one retry) or LLM_INVALID_RESPONSE (not the schema).
public interface IIngredientParser
{
    bool Enabled { get; }

    Task<IReadOnlyList<ParsedItem>> ParseAsync(string text, IReadOnlyList<string> ingredientNames, CancellationToken cancellationToken);
}
