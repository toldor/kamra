using KamraApp.Application.Cookings;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KamraApp.Infrastructure.Persistence;

public sealed class CookingRepository(KamraDbContext db) : ICookingRepository
{
    private const string RequestIndex = "IX_Cookings_HouseholdId_RequestId";

    public Task<Cooking?> FindByRequestAsync(Guid householdId, Guid requestId, CancellationToken cancellationToken) =>
        db.Cookings.AsNoTracking().Include(c => c.Lines)
            .SingleOrDefaultAsync(c => c.HouseholdId == householdId && c.RequestId == requestId, cancellationToken);

    public void Add(Cooking cooking, IEnumerable<StockMovement> movements)
    {
        db.Cookings.Add(cooking);
        db.StockMovements.AddRange(movements);
    }

    // One transaction (SaveChanges). After a failure the tracked, already deducted items are dropped, so the
    // caller's next query reads the database, not the rolled-back changes.
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw PantryErrors.ItemModified();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: RequestIndex,
        })
        {
            db.ChangeTracker.Clear();
            throw new DuplicateCookingRequestException(exception);
        }
    }
}
