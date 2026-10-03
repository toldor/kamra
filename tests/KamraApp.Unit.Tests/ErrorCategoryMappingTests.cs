using KamraApp.Api.ErrorHandling;
using KamraApp.Application.Common;

namespace KamraApp.Unit.Tests;

public class ErrorCategoryMappingTests
{
    [Theory]
    [InlineData(ErrorCategory.Validation, 400)]
    [InlineData(ErrorCategory.Unauthorized, 401)]
    [InlineData(ErrorCategory.Forbidden, 403)]
    [InlineData(ErrorCategory.NotFound, 404)]
    [InlineData(ErrorCategory.Conflict, 409)]
    [InlineData(ErrorCategory.RateLimited, 429)]
    [InlineData(ErrorCategory.BadGateway, 502)]
    [InlineData(ErrorCategory.Unavailable, 503)]
    public void Each_category_maps_to_its_ADR_0007_status(ErrorCategory category, int expectedStatus)
    {
        AppExceptionHandler.ToStatusCode(category).Should().Be(expectedStatus);
    }
}
