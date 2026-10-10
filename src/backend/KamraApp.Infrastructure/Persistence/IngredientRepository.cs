using KamraApp.Application.Ingredients;
using KamraApp.Domain.Ingredients;

namespace KamraApp.Infrastructure.Persistence;

public sealed class IngredientRepository(KamraDbContext db) : IIngredientRepository
{
    public Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {db.GetHashCode()}");

    public Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
