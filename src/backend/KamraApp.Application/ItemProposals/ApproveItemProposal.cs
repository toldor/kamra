using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Domain.ItemProposals;

namespace KamraApp.Application.ItemProposals;

// US-2: the (possibly corrected) proposal becomes a new pantry item, always a new one (no merging). The approval
// runs once (ADR-0007, ADR-0008): the Domain decides, the new item and the decision are saved together with the
// proposal's xmin check; a repeat returns the first pantry item, approving a rejected one is a conflict (409).
public sealed class ApproveItemProposal(IItemProposalRepository proposals, AddPantryItem addPantryItem, IPantryRepository pantry,
    ICurrentHousehold household, TimeProvider time)
{
    public async Task<ApprovedItemProposal> ExecuteAsync(Guid id, AddPantryItemRequest? request, CancellationToken cancellationToken)
    {
        var householdId = household.HouseholdId;
        var proposal = await proposals.FindAsync(householdId, id, cancellationToken) ?? throw ItemProposalErrors.NotFound();
        if (proposal.TransitionTo(ItemProposalState.Approved) != ItemProposalTransition.Changed)
        {
            return await DecidedAsync(proposal, cancellationToken);
        }

        // Validation, the ingredient check, the lock and the new item, as in the manual form; not saved yet.
        var (item, ingredientName) = await addPantryItem.PrepareAsync(request, cancellationToken);
        var now = time.GetUtcNow();
        proposal.Approve(item.Id, now);
        if (!await proposals.TrySaveAsync(cancellationToken))
        {
            // Another request decided first; nothing of ours was saved.
            var current = await proposals.ReloadAsync(householdId, id, cancellationToken) ?? throw ItemProposalErrors.NotFound();
            return await DecidedAsync(current, cancellationToken);
        }

        return new ApprovedItemProposal(PantryItemResponse.From(item, ingredientName, Today.Of(now)), Created: true);
    }

    // A decided proposal: the first approval's item again (as it is now, even if used up since), or a conflict.
    private async Task<ApprovedItemProposal> DecidedAsync(ItemProposal proposal, CancellationToken cancellationToken)
    {
        if (proposal.TransitionTo(ItemProposalState.Approved) != ItemProposalTransition.Repeated)
        {
            throw ItemProposalErrors.StateConflict();
        }

        var entry = await pantry.FindAsync(household.HouseholdId, proposal.PantryItemId!.Value, cancellationToken)
            ?? throw ItemProposalErrors.NotFound();
        return new ApprovedItemProposal(PantryItemResponse.From(entry.Item, entry.IngredientName, Today.Of(time.GetUtcNow())), Created: false);
    }
}
