using KamraApp.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.Pantry;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public IReadOnlyList<CategoryResponse> List([FromServices] ListCategories listCategories) => listCategories.Execute();
}
