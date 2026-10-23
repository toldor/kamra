using KamraApp.Application.Common;

namespace KamraApp.Application.Ingredients;

// N7 (M11, ADR-0003): a household's own ingredient. Its name may not match an active system ingredient or
// another own one of the household (normalized); another household's is no conflict.
public sealed class CreateIngredient(IOwnIngredientRepository ingredients, ICurrentHousehold household)
{
    public Task<IngredientResponse> ExecuteAsync(CreateIngredientRequest? request, CancellationToken cancellationToken)
    {
        _ = (ingredients, household);
        throw new NotImplementedException();
    }
}

public static class IngredientErrors
{
    public static ConflictException NameTaken() =>
        new("INGREDIENT_NAME_TAKEN", "Ilyen nevű hozzávaló már van. Válaszd ki a listából.");
}
