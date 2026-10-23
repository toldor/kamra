using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Ingredients;

// N7 (M11, ADR-0003): a household's own ingredient. Its name may not match an active system ingredient or
// another own one of the household (normalized); another household's is no conflict.
public sealed class CreateIngredient(IOwnIngredientRepository ingredients, ICurrentHousehold household)
{
    public async Task<IngredientResponse> ExecuteAsync(CreateIngredientRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var name = Ingredient.Tidy(valid.Name!);
        if (await ingredients.NameTakenAsync(householdId, Ingredient.Normalize(name), cancellationToken))
        {
            throw IngredientErrors.NameTaken();
        }

        var ingredient = Ingredient.CreateOwn(householdId, name, PantryInput.Parse<Dimension>(valid.Dimension!),
            PantryInput.Parse<Category>(valid.DefaultCategory!));
        ingredients.Add(ingredient);
        await ingredients.SaveChangesAsync(cancellationToken);
        return new IngredientResponse(ingredient.Id, ingredient.Name, ingredient.Dimension, ingredient.DefaultCategory, Minimum: null);
    }
}

public static class IngredientErrors
{
    public static ConflictException NameTaken() =>
        new("INGREDIENT_NAME_TAKEN", "Ilyen nevű hozzávaló már van. Válaszd ki a listából.");
}
