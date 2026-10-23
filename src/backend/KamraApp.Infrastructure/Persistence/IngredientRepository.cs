using KamraApp.Application.Ingredients;
using KamraApp.Domain.Ingredients;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KamraApp.Infrastructure.Persistence;

// ADR-0003: system ingredients and the household's own ones; retired ones cannot be chosen.
public sealed class IngredientRepository(KamraDbContext db) : IIngredientRepository, IOwnIngredientRepository
{
    private const string OwnNameIndex = "IX_Ingredients_HouseholdId_NormalizedName";

    // M11: an active system ingredient or one of the household's own ones.
    public Task<bool> NameTakenAsync(Guid householdId, string normalizedName, CancellationToken cancellationToken) =>
        Visible(householdId).AnyAsync(i => i.NormalizedName == normalizedName, cancellationToken);

    public void Add(Ingredient ingredient) => db.Ingredients.Add(ingredient);

    // A concurrent request saved the same own name first (the partial unique index).
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveAndCommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: OwnNameIndex,
        })
        {
            db.ChangeTracker.Clear();
            throw IngredientErrors.NameTaken();
        }
    }

    public async Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken) =>
        await Visible(householdId)
            .Where(i => normalizedSearch == null || i.NormalizedName.Contains(normalizedSearch))
            .ToListAsync(cancellationToken);

    public Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        Visible(householdId).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

    private IQueryable<Ingredient> Visible(Guid householdId) =>
        db.Ingredients.AsNoTracking().Where(i => (i.HouseholdId == null || i.HouseholdId == householdId) && i.RetiredAt == null);
}
