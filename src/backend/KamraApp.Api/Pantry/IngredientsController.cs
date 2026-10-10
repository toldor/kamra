using KamraApp.Application.Ingredients;
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
        listIngredients.ExecuteAsync(search, cancellationToken);
}
