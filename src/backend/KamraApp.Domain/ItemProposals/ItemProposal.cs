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
    public const int ExpiryTextMaxLength = 100;

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
        DateOnly? expiryDate, bool expiryEstimated, string? expiryText, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (amount is <= 0)
        {
            throw new ArgumentException("The amount must be positive.", nameof(amount));
        }

        // The LLM wrote these parts, not the user: a long one is cut, it does not fail the whole sentence.
        var trimmed = text.Trim();
        var expiryWords = expiryText?.Trim();
        return new ItemProposal
        {
            Id = Guid.CreateVersion7(),
            HouseholdId = householdId,
            Text = trimmed.Length <= TextMaxLength ? trimmed : trimmed[..TextMaxLength],
            IngredientId = ingredientId,
            Amount = amount,
            Unit = unit,
            ExpiryDate = expiryDate,
            ExpiryEstimated = expiryEstimated,
            ExpiryText = expiryWords is { Length: > ExpiryTextMaxLength } ? expiryWords[..ExpiryTextMaxLength] : expiryWords,
            State = ItemProposalState.Pending,
            SubmittedAt = now,
        };
    }

    public ItemProposalTransition Approve(Guid pantryItemId, DateTimeOffset now)
    {
        var transition = Resolve(ItemProposalState.Approved, now);
        if (transition == ItemProposalTransition.Changed)
        {
            PantryItemId = pantryItemId;
        }

        return transition;
    }

    public ItemProposalTransition Reject(DateTimeOffset now) => Resolve(ItemProposalState.Rejected, now);

    // The decision a change to the target would be, without making it: a use case knows it before preparing anything.
    public ItemProposalTransition TransitionTo(ItemProposalState target) =>
        State == target ? ItemProposalTransition.Repeated
        : State == ItemProposalState.Pending ? ItemProposalTransition.Changed
        : ItemProposalTransition.Conflict;

    private ItemProposalTransition Resolve(ItemProposalState target, DateTimeOffset now)
    {
        var transition = TransitionTo(target);
        if (transition == ItemProposalTransition.Changed)
        {
            State = target;
            ResolvedAt = now;
        }

        return transition;
    }
}
