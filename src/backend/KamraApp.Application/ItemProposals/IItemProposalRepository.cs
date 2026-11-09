using KamraApp.Domain.ItemProposals;

namespace KamraApp.Application.ItemProposals;

// Aggregate-level port (AGENTS.md): every read takes the household.
public interface IItemProposalRepository
{
    void AddRange(IEnumerable<ItemProposal> proposals);

    // Tracked: the Domain decides on it, and the save checks that nobody decided since the read (xmin, ADR-0008).
    Task<ItemProposal?> FindAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // From the database again, not the tracked copy: after a lost race, to see the other request's decision.
    Task<ItemProposal?> ReloadAsync(Guid householdId, Guid id, CancellationToken cancellationToken);

    // Saves every change of the request (an approval's new pantry item too) and commits. False, with nothing saved,
    // when another request decided the proposal since it was read.
    Task<bool> TrySaveAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
