using KamraApp.Application.ShoppingList;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.ShoppingList;

// US-5. Thin controller: validation, household scoping, merging and locking live in the use cases (D5).
[ApiController]
[Route("api/v1/shopping-list")]
public sealed class ShoppingListController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public Task<IReadOnlyList<ShoppingListItemResponse>> List([FromServices] ListShoppingList listShoppingList, CancellationToken cancellationToken) =>
        listShoppingList.ExecuteAsync(cancellationToken);

    // One row per ingredient: a new amount adds up (upsert), so the answer is the row, 200.
    [HttpPost("items")]
    [Produces("application/json")]
    public Task<ShoppingListItemResponse> Add(AddShoppingListItemRequest? request, [FromServices] AddShoppingListItem addShoppingListItem,
        CancellationToken cancellationToken) =>
        addShoppingListItem.ExecuteAsync(request, cancellationToken);

    [HttpPut("items/{id:guid}")]
    [Produces("application/json")]
    public Task<ShoppingListItemResponse> Update(Guid id, UpdateShoppingListItemRequest? request,
        [FromServices] UpdateShoppingListItem updateShoppingListItem, CancellationToken cancellationToken) =>
        updateShoppingListItem.ExecuteAsync(id, request, cancellationToken);

    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteShoppingListItem deleteShoppingListItem,
        CancellationToken cancellationToken)
    {
        await deleteShoppingListItem.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
