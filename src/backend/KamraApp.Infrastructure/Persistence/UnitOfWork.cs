using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// D6: IngredientLocks may have opened a transaction on the shared DbContext; the save commits it. Whatever
// happens, the transaction ends here (an uncommitted one rolls back), so a caller that reads after a failed save
// (ConfirmCooking, ADR-0008 3.) reads outside it.
internal static class UnitOfWork
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
