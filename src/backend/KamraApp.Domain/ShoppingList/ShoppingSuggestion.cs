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
    private ShoppingSuggestion(Guid id, Guid householdId, Guid ingredientId, SuggestionTrigger trigger, DateTimeOffset createdAt)
    {
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

    // Accepted or rejected.
    public DateTimeOffset? ResolvedAt { get; private set; }

    // A rejected suggestion whose ingredient got back above the condition (M3).
    public DateTimeOffset? ClearedAt { get; private set; }

    // data_model.md: an open one, or a rejected one not cleared yet, stops a new suggestion for the ingredient.
    public bool Blocks => throw new NotImplementedException();

    public static ShoppingSuggestion Create(Guid householdId, Guid ingredientId, SuggestionTrigger trigger, DateTimeOffset now) =>
        throw new NotImplementedException();

    public SuggestionTransition Accept(DateTimeOffset now) => throw new NotImplementedException();

    public SuggestionTransition Reject(DateTimeOffset now) => throw new NotImplementedException();

    public void Clear(DateTimeOffset now) => throw new NotImplementedException();
}
