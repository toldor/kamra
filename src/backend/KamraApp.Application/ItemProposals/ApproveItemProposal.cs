using KamraApp.Application.Common;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.ItemProposals;

// US-2: the (possibly corrected) proposal becomes a new pantry item, always a new one (no merging). The approval
// runs once (ADR-0007, ADR-0008 4.): the pantry item and the state change are saved in one transaction, a repeat
// returns the first pantry item, approving a rejected proposal is ITEM_PROPOSAL_STATE_CONFLICT.
public sealed class ApproveItemProposal(IItemProposalRepository proposals, AddPantryItem addPantryItem, IPantryRepository pantry,
    ICurrentHousehold household, TimeProvider time)
{
    public Task<ApprovedItemProposal> ExecuteAsync(Guid id, AddPantryItemRequest? request, CancellationToken cancellationToken)
    {
        _ = (proposals, addPantryItem, pantry, household, time); // red stage (spec-first): the T1 implementation uses them
        throw new NotImplementedException();
    }
}
