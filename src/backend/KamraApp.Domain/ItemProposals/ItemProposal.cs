using KamraApp.Domain.Quantities;

namespace KamraApp.Domain.ItemProposals;

public enum ItemProposalState
{
    Pending,
    Approved,
    Rejected,
}

// ADR-0007 / ADR-0008 4.: the same decision again is a repeat (same result), the opposite one a conflict (409).
public enum ItemProposalTransition
{
    Changed,
    Repeated,
    Conflict,
}

// CONTEXT.md, Tételjavaslat: one item extracted from the user's sentence; it becomes a pantry item only when the
// user approves it (US-2). The whole sentence is not stored, only the part this item came from (data minimisation).
public sealed class ItemProposal
{
    public const int TextMaxLength = 200;

    private ItemProposal()
    {
        Text = "";
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public string Text { get; private set; }

    // Null: the LLM found no matching ingredient ("-"); the user picks or creates one before approving.
    public Guid? IngredientId { get; private set; }

    // In the entered unit; null when the sentence gave none.
    public decimal? Amount { get; private set; }

    public Unit? Unit { get; private set; }

    public DateOnly? ExpiryDate { get; private set; }

    public bool ExpiryEstimated { get; private set; }

    // The expiry words that were not a recognised form, to explain the estimate on the card.
    public string? ExpiryText { get; private set; }

    public ItemProposalState State { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    // The pantry item the approval created; a repeated approval returns it (US-2, idempotent).
    public Guid? PantryItemId { get; private set; }

    public static ItemProposal Create(Guid householdId, string text, Guid? ingredientId, decimal? amount, Unit? unit,
        DateOnly? expiryDate, bool expiryEstimated, string? expiryText, DateTimeOffset now) =>
        throw new NotImplementedException();

    public ItemProposalTransition Approve(Guid pantryItemId, DateTimeOffset now) => throw new NotImplementedException();

    public ItemProposalTransition Reject(DateTimeOffset now) => throw new NotImplementedException();
}
