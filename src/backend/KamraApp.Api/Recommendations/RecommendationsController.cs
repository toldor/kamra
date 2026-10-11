using KamraApp.Application.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Recommendations;

// US-3. Thin controller: the ranking lives in the Domain (Recommender).
[ApiController]
[Route("api/v1/recommendations")]
public sealed class RecommendationsController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public Task<RecommendationsResponse> Get([FromServices] GetRecommendations getRecommendations, CancellationToken cancellationToken) =>
        getRecommendations.ExecuteAsync(cancellationToken);
}
