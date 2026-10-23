namespace KamraApp.Domain.ShoppingList;

public enum SuggestionState
{
    Open,
    Accepted,
    Rejected,
}

// ADR-0007 / ADR-0008: the same decision again is a repeat (200, same result), the opposite one a conflict (409).
public enum SuggestionTransition
{
    Changed,
    Repeated,
    Conflict,
}

// CONTEXT.md, Bevásárlójavaslat: offered by the system, on the list only after the user accepts it.
public sealed class ShoppingSuggestion
{
    private ShoppingSuggestion(Guid id, Guid householdId, Guid ingredientId, SuggestionTrigger trigger, DateTimeOffset createdAt, Guid? cookingId)
    {
        CookingId = cookingId;
        Id = id;
        HouseholdId = householdId;
        IngredientId = ingredientId;
        Trigger = trigger;
        State = SuggestionState.Open;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid IngredientId { get; private set; }

    public SuggestionTrigger Trigger { get; private set; }

    public SuggestionState State { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // The cooking that used the ingredient up, so the reason can name the recipe (scope_contract.md US-5).
    public Guid? CookingId { get; private set; }

    // Accepted or rejected.
    public DateTimeOffset? ResolvedAt { get; private set; }

    // A rejected suggestion whose ingredient got back above the condition (scope_contract.md US-5).
    public DateTimeOffset? ClearedAt { get; private set; }

    // data_model.md: an open one, or a rejected one not cleared yet, stops a new suggestion for the ingredient.
    public bool Blocks => State == SuggestionState.Open || (State == SuggestionState.Rejected && ClearedAt is null);

    public static ShoppingSuggestion Create(Guid householdId, Guid ingredientId, SuggestionTrigger trigger, DateTimeOffset now,
        Guid? cookingId = null) =>
        new(Guid.CreateVersion7(), householdId, ingredientId, trigger, now, cookingId);

    public SuggestionTransition Accept(DateTimeOffset now) => Resolve(SuggestionState.Accepted, now);

    public SuggestionTransition Reject(DateTimeOffset now) => Resolve(SuggestionState.Rejected, now);

    public void Clear(DateTimeOffset now)
    {
        if (State != SuggestionState.Rejected || ClearedAt is not null)
        {
            throw new InvalidOperationException("Only a rejected, not yet cleared suggestion can be cleared.");
        }

        ClearedAt = now;
    }

    private SuggestionTransition Resolve(SuggestionState target, DateTimeOffset now)
    {
        if (State == target)
        {
            return SuggestionTransition.Repeated;
        }

        if (State != SuggestionState.Open)
        {
            return SuggestionTransition.Conflict;
        }

        State = target;
        ResolvedAt = now;
        return SuggestionTransition.Changed;
    }
}
