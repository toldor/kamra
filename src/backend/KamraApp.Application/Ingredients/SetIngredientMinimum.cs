using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.ShoppingList;

namespace KamraApp.Application.Ingredients;

// US-5, CONTEXT.md Minimumszint: set (or remove, both fields empty) the household's minimum for an ingredient it
// can see. Setting it above the current total suggests at once (M1); removing it clears a rejected one (M3).
public sealed class SetIngredientMinimum(IIngredientRepository ingredients, IShoppingListRepository shopping, IPantryRepository pantry,
    IIngredientLocks locks, SuggestionEvaluator suggestions, ICurrentHousehold household, TimeProvider time)
{
    public Task ExecuteAsync(Guid ingredientId, SetIngredientMinimumRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {(ingredients, shopping, pantry, locks, suggestions, household, time)}");
}
