using KamraApp.Domain.Pantry;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5: what a change of an ingredient's total means for the shopping suggestions. Called inside the ingredient
// lock (ADR-0008 6.), before the caller saves, so the suggestion is saved with the change. A rejected suggestion
// back above its condition is cleared first (M3); a new one is made only when nothing blocks it and the
// ingredient is not on the list unchecked (M15).
public sealed class SuggestionEvaluator(IShoppingListRepository shopping)
{
    // True when a new suggestion was made (M5: the cooking's summary names it).
    public async Task<bool> AfterQuantityChangeAsync(Guid householdId, Guid ingredientId, decimal before, decimal after, MovementReason reason,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var minimum = (await shopping.FindMinimumAsync(householdId, ingredientId, cancellationToken))?.MinimumQuantity;
        var blocking = await ClearAsync(householdId, ingredientId, after, minimum, now, cancellationToken);
        return SuggestionRules.OnDecrease(before, after, minimum, reason) is { } trigger
            && await SuggestAsync(householdId, ingredientId, trigger, blocking, now, cancellationToken);
    }

    // M1, M3: after a minimum was set (or removed, null).
    public async Task AfterMinimumChangeAsync(Guid householdId, Guid ingredientId, decimal total, decimal? minimum, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blocking = await ClearAsync(householdId, ingredientId, total, minimum, now, cancellationToken);
        if (minimum is { } limit && SuggestionRules.OnMinimumSet(total, limit) is { } trigger)
        {
            await SuggestAsync(householdId, ingredientId, trigger, blocking, now, cancellationToken);
        }
    }

    // The blocking suggestion that is left after clearing a rejected one back above its condition.
    private async Task<ShoppingSuggestion?> ClearAsync(Guid householdId, Guid ingredientId, decimal total, decimal? minimum, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blocking = await shopping.FindBlockingSuggestionAsync(householdId, ingredientId, cancellationToken);
        if (blocking is { State: SuggestionState.Rejected } && SuggestionRules.Clears(blocking.Trigger, total, minimum))
        {
            blocking.Clear(now);
            return null;
        }

        return blocking;
    }

    private async Task<bool> SuggestAsync(Guid householdId, Guid ingredientId, SuggestionTrigger trigger, ShoppingSuggestion? blocking,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (blocking is not null
            || await shopping.FindItemByIngredientAsync(householdId, ingredientId, cancellationToken) is { Checked: false })
        {
            return false;
        }

        shopping.AddSuggestion(ShoppingSuggestion.Create(householdId, ingredientId, trigger, now));
        return true;
    }
}
