namespace KamraApp.Domain.Pantry;

// Append-only log of every quantity change (CONTEXT.md, Készletmozgás-napló; data_model.md).
public sealed class StockMovement
{
    private StockMovement(Guid id, Guid householdId, Guid pantryItemId, decimal delta, MovementReason reason,
        DateOnly expiryDateAtMovement, DateTimeOffset occurredAt)
    {
        Id = id;
        HouseholdId = householdId;
        PantryItemId = pantryItemId;
        Delta = delta;
        Reason = reason;
        ExpiryDateAtMovement = expiryDateAtMovement;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid PantryItemId { get; private set; }

    // Signed change in base units: + added, - decreased.
    public decimal Delta { get; private set; }

    public MovementReason Reason { get; private set; }

    // The item's expiry at the time of the movement, so a later expiry edit does not rewrite the metrics.
    public DateOnly ExpiryDateAtMovement { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    internal static StockMovement Record(PantryItem item, decimal delta, MovementReason reason, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), item.HouseholdId, item.Id, delta, reason, item.ExpiryDate, now);
}

// CONTEXT.md, Csökkenési ok: Consumed (elfogyott), Discarded (kidobtam), Corrected (hibás rögzítés).
public enum MovementReason
{
    Added,
    Consumed,
    Discarded,
    Corrected,
}
