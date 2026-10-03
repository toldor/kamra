using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace KamraApp.Api.ErrorHandling;

// The global antiforgery filter answers a bare 400; this turns it into the common ProblemDetails
// shape with a stable code (ADR-0007). It must run before [ApiController]'s client-error filter
// (order -2000), which would otherwise replace the result with a generic 400.
public sealed class AntiforgeryProblemFilter(ProblemDetailsFactory problems) : IAlwaysRunResultFilter
{
    public const int FilterOrder = -3000;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            var problem = problems.CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest);
            problem.Title = "A kérés biztonsági ellenőrzése nem sikerült. Frissítsd az oldalt, és próbáld újra.";
            problem.Extensions[AppExceptionHandler.CodeKey] = "ANTIFORGERY_TOKEN_INVALID";
            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
