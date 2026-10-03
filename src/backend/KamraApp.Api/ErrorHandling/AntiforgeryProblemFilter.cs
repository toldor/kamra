using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KamraApp.Api.ErrorHandling;

// The global antiforgery filter answers a bare 400; this writes the common ProblemDetails with a
// stable code instead (ADR-0007). It must run before [ApiController]'s client-error filter
// (order -2000), which would otherwise replace the result with a generic 400.
public sealed class AntiforgeryProblemFilter : IAsyncAlwaysRunResultFilter
{
    public const int FilterOrder = -3000;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            await AppExceptionHandler.WriteProblemAsync(context.HttpContext, StatusCodes.Status400BadRequest,
                "ANTIFORGERY_TOKEN_INVALID", "A kérés biztonsági ellenőrzése nem sikerült. Frissítsd az oldalt, és próbáld újra.");
            context.Cancel = true;
            return;
        }

        await next();
    }
}
