using KamraApp.Application.Recipes;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Recipes;

// US-3, the Receptek page (T3 mini-spec N1-N4). There is no GET /recipes/{id}: no screen needs it (N1).
[ApiController]
[Route("api/v1/recipes")]
public sealed class RecipesController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public Task<IReadOnlyList<RecipeResponse>> List([FromServices] ListRecipes listRecipes, CancellationToken cancellationToken) =>
        listRecipes.ExecuteAsync(cancellationToken);

    // 201 with the new recipe's row; no Location, as there is no single-recipe endpoint (N1).
    [HttpPost]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateRecipeRequest? request, [FromServices] CreateRecipe createRecipe,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await createRecipe.ExecuteAsync(request, cancellationToken));

    // M10: 204, a repeated archiving too.
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Archive(Guid id, [FromServices] ArchiveRecipe archiveRecipe, CancellationToken cancellationToken)
    {
        await archiveRecipe.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
