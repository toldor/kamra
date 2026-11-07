using KamraApp.Application.Common;
using KamraApp.Domain.ItemProposals;

namespace KamraApp.Application.ItemProposals;

// US-2: the user skips a proposal ("Kihagyom"); nothing is saved to the pantry. The Domain decides (ADR-0008):
// a repeat changes nothing, rejecting an approved proposal is ITEM_PROPOSAL_STATE_CONFLICT.
public sealed class RejectItemProposal(IItemProposalRepository proposals, ICurrentHousehold household, TimeProvider time)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var householdId = household.HouseholdId;
        var proposal = await proposals.FindAsync(householdId, id, cancellationToken) ?? throw ItemProposalErrors.NotFound();
        var transition = proposal.Reject(time.GetUtcNow());
        if (transition == ItemProposalTransition.Changed && !await proposals.TrySaveAsync(cancellationToken))
        {
            // Another request decided first: the same decision is a repeat, an approval a conflict.
            var current = await proposals.ReloadAsync(householdId, id, cancellationToken) ?? throw ItemProposalErrors.NotFound();
            transition = current.TransitionTo(ItemProposalState.Rejected);
        }

        if (transition == ItemProposalTransition.Conflict)
        {
            throw ItemProposalErrors.StateConflict();
        }
    }
}
