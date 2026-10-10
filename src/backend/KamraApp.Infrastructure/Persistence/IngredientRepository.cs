using KamraApp.Application.Ingredients;
using KamraApp.Domain.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// ADR-0003: system ingredients and the household's own ones; retired ones cannot be chosen.
public sealed class IngredientRepository(KamraDbContext db) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken) =>
        await Visible(householdId)
            .Where(i => normalizedSearch == null || i.NormalizedName.Contains(normalizedSearch))
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

    public Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        Visible(householdId).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

    private IQueryable<Ingredient> Visible(Guid householdId) =>
        db.Ingredients.AsNoTracking().Where(i => (i.HouseholdId == null || i.HouseholdId == householdId) && i.RetiredAt == null);
}
