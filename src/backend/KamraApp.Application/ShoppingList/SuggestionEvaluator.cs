using KamraApp.Domain.Pantry;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5: what a change of an ingredient's total means for the shopping suggestions (data_model.md,
// ShoppingSuggestions). Called inside the ingredient lock (ADR-0008 6.), before the caller saves, so the suggestion
// is saved with the change. A rejected suggestion back above its condition is cleared first.
public sealed class SuggestionEvaluator(IShoppingListRepository shopping)
{
    // True when a new suggestion was made (the cooking's summary names it). A cooking passes its id, so the
    // reason can name the recipe.
    public async Task<bool> AfterQuantityChangeAsync(Guid householdId, Guid ingredientId, decimal before, decimal after, MovementReason reason,
        DateTimeOffset now, CancellationToken cancellationToken, Guid? cookingId = null)
    {
        var minimum = (await shopping.FindMinimumAsync(householdId, ingredientId, cancellationToken))?.MinimumQuantity;
        var blocking = await BlockingAfterClearingAsync(householdId, ingredientId, after, minimum, now, cancellationToken);
        return SuggestionRules.OnDecrease(before, after, minimum, reason) is { } trigger
            && await SuggestAsync(householdId, ingredientId, trigger, blocking, now, cookingId, cancellationToken);
    }

    // After a minimum was set (or removed, null).
    public async Task AfterMinimumChangeAsync(Guid householdId, Guid ingredientId, decimal total, decimal? minimum, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blocking = await BlockingAfterClearingAsync(householdId, ingredientId, total, minimum, now, cancellationToken);
        if (minimum is { } limit && SuggestionRules.OnMinimumSet(total, limit) is { } trigger)
        {
            await SuggestAsync(householdId, ingredientId, trigger, blocking, now, cookingId: null, cancellationToken);
        }
    }

    // Clears a rejected suggestion that is back above its condition; returns the suggestion that still blocks.
    private async Task<ShoppingSuggestion?> BlockingAfterClearingAsync(Guid householdId, Guid ingredientId, decimal total, decimal? minimum,
        DateTimeOffset now, CancellationToken cancellationToken)
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
        DateTimeOffset now, Guid? cookingId, CancellationToken cancellationToken)
    {
        if (SuggestionRules.IsSuppressed(blocking, await shopping.FindItemByIngredientAsync(householdId, ingredientId, cancellationToken)))
        {
            return false;
        }

        shopping.AddSuggestion(ShoppingSuggestion.Create(householdId, ingredientId, trigger, now, cookingId));
        return true;
    }
}
