using KamraApp.Domain.Ingredients;

namespace KamraApp.Application.Ingredients;

// M11: a household's own ingredients. The save reports a concurrent duplicate name (the unique index) as
// INGREDIENT_NAME_TAKEN. A second port of the ingredient aggregate next to IIngredientRepository, so its callers
// and test fakes stay as they are (T3 review S9); the same class implements both.
public interface IOwnIngredientRepository
{
    // True when an active system ingredient or one of the household's own ones has this normalized name.
    Task<bool> NameTakenAsync(Guid householdId, string normalizedName, CancellationToken cancellationToken);

    void Add(Ingredient ingredient);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
