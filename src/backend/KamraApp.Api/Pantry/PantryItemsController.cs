using KamraApp.Application.Pantry;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Pantry;

// US-1. Thin controller: validation, household scoping and concurrency live in the use cases.
[ApiController]
[Route("api/v1/pantry-items")]
public sealed class PantryItemsController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    // Separate parameters keep the camelCase names in the OpenAPI contract; the use case validates them.
    public Task<IReadOnlyList<PantryItemResponse>> List([FromQuery] string? search, [FromQuery] string? category,
        [FromQuery] bool? expiringSoon, [FromServices] ListPantryItems listPantryItems, CancellationToken cancellationToken) =>
        listPantryItems.ExecuteAsync(new ListPantryItemsQuery { Search = search, Category = category, ExpiringSoon = expiringSoon },
            cancellationToken);

    [HttpPost]
    [ProducesResponseType<PantryItemResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(AddPantryItemRequest? request, [FromServices] AddPantryItem addPantryItem,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await addPantryItem.ExecuteAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Produces("application/json")]
    public Task<PantryItemResponse> Update(Guid id, UpdatePantryItemRequest? request, [FromServices] UpdatePantryItem updatePantryItem,
        CancellationToken cancellationToken) =>
        updatePantryItem.ExecuteAsync(id, request, cancellationToken);
}
