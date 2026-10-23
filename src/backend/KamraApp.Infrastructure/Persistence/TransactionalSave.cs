using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// ADR-0008 6.: IngredientLocks may have opened a transaction on the shared DbContext; the save commits it, which also
// releases the lock. Whatever happens, the transaction ends here (an uncommitted one rolls back), so a caller that
// reads after a failed save (ConfirmCooking, ADR-0008 3.) reads outside it. A use case that fails inside the lock
// before saving leaves the transaction to the end of the request (the DbContext disposal rolls it back); reads in
// between still see every committed change (READ COMMITTED).
internal static class TransactionalSave
{
    public static async Task SaveAndCommitAsync(this KamraDbContext db, CancellationToken cancellationToken)
    {
        var transaction = db.Database.CurrentTransaction;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
