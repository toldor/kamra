using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;

namespace KamraApp.Application.Ingredients;

// US-5, CONTEXT.md Minimumszint: set (or remove, both fields empty) the household's minimum for an ingredient it
// can see. Setting it above the current total suggests at once (M1); removing it clears a rejected one (M3).
public sealed class SetIngredientMinimum(IIngredientRepository ingredients, IShoppingListRepository shopping, IPantryRepository pantry,
    IIngredientLocks locks, SuggestionEvaluator suggestions, ICurrentHousehold household, TimeProvider time)
{
    public async Task ExecuteAsync(Guid ingredientId, SetIngredientMinimumRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var ingredient = await ingredients.FindVisibleAsync(householdId, ingredientId, cancellationToken) ?? throw PantryErrors.IngredientNotFound();
        var unit = ShoppingListInput.ParseUnit(valid.Unit);
        if (unit is { } given)
        {
            PantryInput.EnsureUnitFits(given, ingredient.Dimension, ingredient.Name);
        }

        await locks.LockAsync(householdId, [ingredient.Id], cancellationToken);
        var minimum = await shopping.FindMinimumAsync(householdId, ingredient.Id, cancellationToken);
        if (valid.Amount is not { } amount)
        {
            if (minimum is not null)
            {
                shopping.RemoveMinimum(minimum);
            }

            minimum = null;
        }
        else if (minimum is null)
        {
            minimum = IngredientMinimum.Create(householdId, ingredient, amount, unit!.Value);
            shopping.AddMinimum(minimum);
        }
        else
        {
            minimum.Change(amount, unit!.Value);
        }

        var total = await pantry.TotalAsync(householdId, ingredient.Id, cancellationToken);
        await suggestions.AfterMinimumChangeAsync(householdId, ingredient.Id, total, minimum?.MinimumQuantity, time.GetUtcNow(), cancellationToken);
        await shopping.SaveChangesAsync(cancellationToken);
    }
}
