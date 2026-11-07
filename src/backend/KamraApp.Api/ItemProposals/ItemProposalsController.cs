using KamraApp.Api.Auth;
using KamraApp.Application.ItemProposals;
using KamraApp.Application.Pantry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KamraApp.Api.ItemProposals;

// US-2 (us2_minispec K3): a sentence becomes item proposals; each one is approved into the pantry or skipped.
[ApiController]
[Route("api/v1/item-proposals")]
public sealed class ItemProposalsController : ControllerBase
{
    // K10: the client shows the quick entry only when the switch is on.
    [HttpGet("availability")]
    [Produces("application/json")]
    public ItemProposalAvailability Availability([FromServices] SubmitItemProposals submitItemProposals) =>
        submitItemProposals.Availability();

    // K11: the only endpoint that calls the LLM is limited per household.
    [HttpPost]
    [EnableRateLimiting(AuthenticationSetup.ItemProposalsRateLimitPolicy)]
    [ProducesResponseType<SubmitItemProposalsResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(SubmitItemProposalsRequest? request, [FromServices] SubmitItemProposals submitItemProposals,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await submitItemProposals.ExecuteAsync(request, cancellationToken));

    // ADR-0007: 201 for the approval that created the pantry item, 200 with the same item for a repeat.
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<PantryItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PantryItemResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id, AddPantryItemRequest? request, [FromServices] ApproveItemProposal approveItemProposal,
        CancellationToken cancellationToken)
    {
        var result = await approveItemProposal.ExecuteAsync(id, request, cancellationToken);
        return StatusCode(result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK, result.Item);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id, [FromServices] RejectItemProposal rejectItemProposal, CancellationToken cancellationToken)
    {
        await rejectItemProposal.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
