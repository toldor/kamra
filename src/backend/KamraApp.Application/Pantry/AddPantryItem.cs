using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Pantry;

// US-1: a new pantry item with an Added movement; a missing expiry date is estimated from the category. US-5: the
// purchase may clear a rejected shopping suggestion (data_model.md, ShoppingSuggestions), inside the ingredient lock (ADR-0008 6.).
public sealed class AddPantryItem(IPantryRepository pantry, IIngredientRepository ingredients, IIngredientLocks locks,
    SuggestionEvaluator suggestions, ICurrentHousehold household, TimeProvider time)
{
    public async Task<PantryItemResponse> ExecuteAsync(AddPantryItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var unit = PantryInput.Parse<Unit>(valid.Unit!);
        var ingredient = await ingredients.FindVisibleAsync(household.HouseholdId, valid.IngredientId!.Value, cancellationToken)
            ?? throw PantryErrors.IngredientNotFound();
        PantryInput.EnsureUnitFits(unit, ingredient.Dimension, ingredient.Name);
        var category = valid.Category is null ? ingredient.DefaultCategory : PantryInput.Parse<Category>(valid.Category);
        PantryInput.EnsureExpiryKnown(category, valid.ExpiryDate);

        var now = time.GetUtcNow();
        var today = Today.Of(now);
        await locks.LockAsync(household.HouseholdId, [ingredient.Id], cancellationToken);
        var before = await pantry.TotalAsync(household.HouseholdId, ingredient.Id, cancellationToken);
        var (item, movement) = PantryItem.Create(household.HouseholdId, ingredient, valid.Amount!.Value, unit, category,
            valid.ExpiryDate, today, now);
        pantry.Add(item, movement);
        await suggestions.AfterQuantityChangeAsync(household.HouseholdId, ingredient.Id, before, before + item.Quantity, movement.Reason, now,
            cancellationToken);
        await pantry.SaveChangesAsync(cancellationToken);

        return PantryItemResponse.From(item, ingredient.Name, today);
    }
}
