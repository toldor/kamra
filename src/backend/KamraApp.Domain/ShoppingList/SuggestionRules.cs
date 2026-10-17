using KamraApp.Domain.Pantry;

namespace KamraApp.Domain.ShoppingList;

// CONTEXT.md, Bevásárlójavaslat: why the suggestion was made (data_model.md, ShoppingSuggestions.Trigger).
public enum SuggestionTrigger
{
    Depleted,
    BelowMinimum,
}

// US-5 rules on an ingredient's total (all pantry items, base units); data_model.md, ShoppingSuggestions.
public static class SuggestionRules
{
    // No new suggestion while a blocking one exists, or while the ingredient is on the list unchecked; a checked
    // (bought) row does not count.
    public static bool IsSuppressed(ShoppingSuggestion? blocking, ShoppingListItem? row) => blocking is not null || row is { Checked: false };

    // Only a Consumed decrease triggers. Reaching 0 is Depleted even when a minimum is set; crossing
    // the minimum from at or above it is BelowMinimum.
    public static SuggestionTrigger? OnDecrease(decimal before, decimal after, decimal? minimum, MovementReason reason) =>
        reason != MovementReason.Consumed || after >= before ? null
        : after == 0 ? SuggestionTrigger.Depleted
        : minimum is { } limit && before >= limit && after < limit ? SuggestionTrigger.BelowMinimum
        : null;

    // Setting a minimum above the current total triggers at once.
    public static SuggestionTrigger? OnMinimumSet(decimal total, decimal minimum) =>
        total < minimum ? SuggestionTrigger.BelowMinimum : null;

    // A rejected suggestion is cleared once the total is back above its condition (or the minimum is gone).
    public static bool Clears(SuggestionTrigger trigger, decimal total, decimal? minimum) => trigger switch
    {
        SuggestionTrigger.Depleted => total > 0,
        _ => minimum is not { } limit || total >= limit,
    };

    // The amount offered on accepting - what is missing up to the minimum; nothing for Depleted.
    public static decimal? SuggestedQuantity(SuggestionTrigger trigger, decimal total, decimal? minimum) =>
        trigger == SuggestionTrigger.BelowMinimum && minimum is { } limit && total < limit ? limit - total : null;
}
