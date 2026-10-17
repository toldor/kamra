using KamraApp.Application.Ingredients;
using KamraApp.Application.ShoppingList;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Pantry;

[ApiController]
[Route("api/v1/ingredients")]
public sealed class IngredientsController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public Task<IReadOnlyList<IngredientResponse>> List([FromQuery] string? search, [FromServices] ListIngredients listIngredients,
        CancellationToken cancellationToken) =>
        listIngredients.ExecuteAsync(new ListIngredientsQuery { Search = search }, cancellationToken);

    // US-5: set the household's minimum, or remove it with an empty body ({}); 204.
    [HttpPut("{id:guid}/minimum")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetMinimum(Guid id, SetIngredientMinimumRequest? request,
        [FromServices] SetIngredientMinimum setIngredientMinimum, CancellationToken cancellationToken)
    {
        await setIngredientMinimum.ExecuteAsync(id, request, cancellationToken);
        return NoContent();
    }
}
