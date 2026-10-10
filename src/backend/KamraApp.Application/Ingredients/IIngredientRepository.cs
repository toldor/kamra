using KamraApp.Domain.Ingredients;

namespace KamraApp.Application.Ingredients;

// ADR-0003: a household sees the system ingredients and its own ones, never retired ones.
public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken);

    Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken);
}
