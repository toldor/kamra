using KamraApp.Application.ItemProposals;
using KamraApp.Domain.ItemProposals;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// Every query filters on the household (QA-1). The decision is saved with the proposal's xmin check (ADR-0008,
// Módosítás 2026-11-07): when another request decided first, the save fails and nothing of this request is kept.
public sealed class ItemProposalRepository(KamraDbContext db) : IItemProposalRepository
{
    public void AddRange(IEnumerable<ItemProposal> proposals) => db.ItemProposals.AddRange(proposals);

    public Task<ItemProposal?> FindAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        db.ItemProposals.SingleOrDefaultAsync(p => p.Id == id && p.HouseholdId == householdId, cancellationToken);

    public Task<ItemProposal?> ReloadAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
        db.ItemProposals.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.HouseholdId == householdId, cancellationToken);

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveAndCommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The transaction is rolled back (TransactionalSave); the tracked changes are dropped with the request.
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAndCommitAsync(cancellationToken);
}
