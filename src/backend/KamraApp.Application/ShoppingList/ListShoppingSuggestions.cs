using KamraApp.Application.Common;
using KamraApp.Domain;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.ShoppingList;

namespace KamraApp.Application.ShoppingList;

// US-5: the open suggestions with their reason and the amount to buy, oldest first.
public sealed class ListShoppingSuggestions(IShoppingListRepository shopping, ICurrentHousehold household)
{
    public async Task<IReadOnlyList<ShoppingSuggestionResponse>> ExecuteAsync(CancellationToken cancellationToken) =>
        [.. (await shopping.ListOpenSuggestionsAsync(household.HouseholdId, cancellationToken))
            .OrderBy(e => e.Suggestion.CreatedAt)
            .ThenBy(e => e.IngredientName, Collation.Hungarian)
            .Select(Respond)];

    // For BelowMinimum every amount is in the minimum's unit; if the minimum was removed meanwhile (the open
    // suggestion stays), the current amount is in the base unit and the other two are empty. A Depleted suggestion
    // has none (the user decides).
    private static ShoppingSuggestionResponse Respond(SuggestionEntry entry)
    {
        var (suggestion, minimum) = (entry.Suggestion, entry.Minimum);
        var unit = suggestion.Trigger == SuggestionTrigger.BelowMinimum ? minimum?.EnteredUnit ?? Units.BaseUnit(entry.Dimension) : (Unit?)null;
        var limit = minimum?.MinimumQuantity;
        return new ShoppingSuggestionResponse(suggestion.Id, suggestion.IngredientId, entry.IngredientName, entry.Dimension, suggestion.Trigger,
            suggestion.CreatedAt, entry.RecipeName, AmountResponse.Of(unit is null ? null : entry.Total, unit), AmountResponse.Of(limit, unit),
            AmountResponse.Of(SuggestionRules.SuggestedQuantity(suggestion.Trigger, entry.Total, limit), unit));
    }
}
