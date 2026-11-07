using KamraApp.Domain.ItemProposals;

namespace KamraApp.Application.ItemProposals;

// Aggregate-level port (AGENTS.md): every read takes the household.
public interface IItemProposalRepository
{
    void AddRange(IEnumerable<ItemProposal> proposals);

    Task<ItemProposal?> FindAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // ADR-0008 4.: Pending -> Approved / Rejected as one conditional update in the current transaction
    // (WHERE State = 'Pending'); false when another request decided first.
    Task<bool> TryApproveAsync(Guid householdId, Guid id, Guid pantryItemId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid householdId, Guid id, DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
