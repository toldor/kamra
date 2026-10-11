using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Pantry;

// US-1: a new pantry item with an Added movement; a missing expiry date is estimated from the category.
public sealed class AddPantryItem(IPantryRepository pantry, IIngredientRepository ingredients, ICurrentHousehold household,
    TimeProvider time)
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
        var (item, movement) = PantryItem.Create(household.HouseholdId, ingredient, valid.Amount!.Value, unit, category,
            valid.ExpiryDate, today, now);
        pantry.Add(item, movement);
        await pantry.SaveChangesAsync(cancellationToken);

        return PantryItemResponse.From(item, ingredient.Name, today);
    }
}
