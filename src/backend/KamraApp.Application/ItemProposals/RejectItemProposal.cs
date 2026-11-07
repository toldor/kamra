using KamraApp.Application.Common;

namespace KamraApp.Application.ItemProposals;

// US-2: the user skips a proposal ("Kihagyom"); nothing is saved to the pantry. A repeat changes nothing,
// rejecting an approved proposal is ITEM_PROPOSAL_STATE_CONFLICT (ADR-0008 4.).
public sealed class RejectItemProposal(IItemProposalRepository proposals, ICurrentHousehold household, TimeProvider time)
{
    public Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = (proposals, household, time); // red stage (spec-first): the T1 implementation uses them
        throw new NotImplementedException();
    }
}
