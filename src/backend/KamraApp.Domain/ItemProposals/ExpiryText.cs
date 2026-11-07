namespace KamraApp.Domain.ItemProposals;

// US-2 / ADR-0009: the LLM returns the expiry words as the user wrote them; the date comes from here, so the
// rules of the scope contract are deterministic and tested. Null: not a recognised form, the caller estimates.
public static class ExpiryText
{
    public static DateOnly? Resolve(string? text, DateOnly today) => throw new NotImplementedException();
}
