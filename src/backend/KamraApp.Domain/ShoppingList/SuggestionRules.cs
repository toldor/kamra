using KamraApp.Domain.Pantry;

namespace KamraApp.Domain.ShoppingList;

// CONTEXT.md, Bevásárlójavaslat: why the suggestion was made (data_model.md, ShoppingSuggestions.Trigger).
public enum SuggestionTrigger
{
    Depleted,
    BelowMinimum,
}

// US-5 rules on an ingredient's total (all pantry items, base units). Whether an existing suggestion or an
// unchecked list item suppresses a new one is the caller's part (M15, data_model.md).
public static class SuggestionRules
{
    // M6, D2: only a Consumed decrease triggers. Reaching 0 is Depleted even when a minimum is set; crossing
    // the minimum from at or above it is BelowMinimum.
    public static SuggestionTrigger? OnDecrease(decimal before, decimal after, decimal? minimum, MovementReason reason) =>
        reason != MovementReason.Consumed || after >= before ? null
        : after == 0 ? SuggestionTrigger.Depleted
        : minimum is { } limit && before >= limit && after < limit ? SuggestionTrigger.BelowMinimum
        : null;

    // M1: setting a minimum above the current total triggers at once.
    public static SuggestionTrigger? OnMinimumSet(decimal total, decimal minimum) =>
        total < minimum ? SuggestionTrigger.BelowMinimum : null;

    // M3: a rejected suggestion is cleared once the total is back above its condition (or the minimum is gone).
    public static bool Clears(SuggestionTrigger trigger, decimal total, decimal? minimum) => trigger switch
    {
        SuggestionTrigger.Depleted => total > 0,
        _ => minimum is not { } limit || total >= limit,
    };

    // M13: the amount offered on accepting - what is missing up to the minimum; nothing for Depleted.
    public static decimal? SuggestedQuantity(SuggestionTrigger trigger, decimal total, decimal? minimum) =>
        trigger == SuggestionTrigger.BelowMinimum && minimum is { } limit && total < limit ? limit - total : null;
}
