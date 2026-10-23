using KamraApp.Application.ShoppingList;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.ShoppingList;

// US-5: accepting and rejecting answer 204, and the same decision again gets the same answer (ADR-0008 4.).
[ApiController]
[Route("api/v1/shopping-suggestions")]
public sealed class ShoppingSuggestionsController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public Task<IReadOnlyList<ShoppingSuggestionResponse>> List([FromServices] ListShoppingSuggestions listShoppingSuggestions,
        CancellationToken cancellationToken) =>
        listShoppingSuggestions.ExecuteAsync(cancellationToken);

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Accept(Guid id, AcceptShoppingSuggestionRequest? request,
        [FromServices] AcceptShoppingSuggestion acceptShoppingSuggestion, CancellationToken cancellationToken)
    {
        await acceptShoppingSuggestion.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id, [FromServices] RejectShoppingSuggestion rejectShoppingSuggestion,
        CancellationToken cancellationToken)
    {
        await rejectShoppingSuggestion.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
