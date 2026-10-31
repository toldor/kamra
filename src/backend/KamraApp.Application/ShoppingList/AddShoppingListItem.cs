using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.ShoppingList;

// US-5 (and the "Hiány a bevásárlólistára" buttons of US-3 and US-4): one row per ingredient; a new amount adds up, a
// checked row starts again, and the sum stays within the input limit (data_model.md, ShoppingListItems).
public sealed class AddShoppingListItem(IShoppingListRepository shopping, IIngredientRepository ingredients, IIngredientLocks locks,
    ICurrentHousehold household, TimeProvider time)
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(AddShoppingListItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var ingredient = await ingredients.FindVisibleAsync(householdId, valid.IngredientId!.Value, cancellationToken)
            ?? throw PantryErrors.IngredientNotFound();
        var unit = ShoppingListInput.UnitFor(ingredient, valid.Unit);

        await locks.LockAsync(householdId, [ingredient.Id], cancellationToken);
        var item = await ShoppingListRows.AddAsync(shopping, householdId, ingredient.Id, valid.Amount, unit, time.GetUtcNow(), cancellationToken);
        await shopping.SaveChangesAsync(cancellationToken);
        return ShoppingListItemResponse.From(item, ingredient.Name);
    }
}
