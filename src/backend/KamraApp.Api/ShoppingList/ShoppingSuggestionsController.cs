using KamraApp.Application.ShoppingList;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.ShoppingList;

// US-5, D5: accepting returns the list row (200), rejecting 204; the same decision again gives the same answer.
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
    [Produces("application/json")]
    public Task<ShoppingListItemResponse> Accept(Guid id, AcceptShoppingSuggestionRequest? request,
        [FromServices] AcceptShoppingSuggestion acceptShoppingSuggestion, CancellationToken cancellationToken) =>
        acceptShoppingSuggestion.ExecuteAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id, [FromServices] RejectShoppingSuggestion rejectShoppingSuggestion,
        CancellationToken cancellationToken)
    {
        await rejectShoppingSuggestion.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
