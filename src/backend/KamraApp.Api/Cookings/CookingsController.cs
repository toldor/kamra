using KamraApp.Application.Cookings;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Cookings;

// US-4. 201 for the first confirmation, 200 with the same body for a repeated one (ADR-0007, ADR-0008).
[ApiController]
[Route("api/v1")]
public sealed class CookingsController : ControllerBase
{
    [HttpGet("recipes/{id:guid}/cooking-preview")]
    [Produces("application/json")]
    public Task<CookingPreviewResponse> Preview(Guid id, [FromQuery] int? servings, [FromServices] PreviewCooking previewCooking,
        CancellationToken cancellationToken) =>
        previewCooking.ExecuteAsync(id, servings, cancellationToken);

    [HttpPost("cookings")]
    [ProducesResponseType<CookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<CookingResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(ConfirmCookingRequest? request, [FromServices] ConfirmCooking confirmCooking,
        CancellationToken cancellationToken)
    {
        var result = await confirmCooking.ExecuteAsync(request, cancellationToken);
        return StatusCode(result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK, result.Cooking);
    }
}
