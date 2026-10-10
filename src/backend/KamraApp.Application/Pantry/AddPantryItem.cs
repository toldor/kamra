using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;

namespace KamraApp.Application.Pantry;

// US-1: a new pantry item with an Added movement; a missing expiry date is estimated from the category.
public sealed class AddPantryItem(IPantryRepository pantry, IIngredientRepository ingredients, ICurrentHousehold household,
    TimeProvider time)
{
    public Task<PantryItemResponse> ExecuteAsync(AddPantryItemRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {pantry.GetHashCode() + ingredients.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}
