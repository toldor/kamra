using KamraApp.Application.Pantry;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Pantry;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public IReadOnlyList<CategoryResponse> List() => ListCategories.Execute();
}
