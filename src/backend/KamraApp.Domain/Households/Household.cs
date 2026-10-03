namespace KamraApp.Domain.Households;

// One account = one household (ADR-0006). The owner is referenced by id only, so the Domain does
// not depend on the identity framework.
public sealed class Household
{
    private Household(Guid id, Guid ownerUserId, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Household Create(Guid ownerUserId, DateTimeOffset now)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("The owner user id must be set.", nameof(ownerUserId));
        }

        // ADR-0005: GUID v7 generated in the Domain, so the id is known before saving.
        return new Household(Guid.CreateVersion7(), ownerUserId, now);
    }
}
