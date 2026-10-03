using KamraApp.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.ErrorHandling;

// ADR-0007: the single place where errors become ProblemDetails - exceptions (TryHandleAsync) and
// framework-generated responses such as an unknown route (Customize). Unknown exceptions never leak
// their message or stack trace; the details go to the log under the correlation id.
public sealed partial class AppExceptionHandler(IProblemDetailsService problemDetails, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public const string CodeKey = "code";
    public const string CorrelationIdKey = "correlationId";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            AppException app => (ToStatusCode(app.Category), app.Code, app.Message),
            // Framework rejections carry their own 4xx status (e.g. 413 request too large).
            BadHttpRequestException bad => (bad.StatusCode, DefaultCodeFor(bad.StatusCode), DefaultTitleFor(bad.StatusCode)),
            _ => (StatusCodes.Status500InternalServerError, DefaultCodeFor(500), DefaultTitleFor(500)),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogFailed(logger, exception, code);
        }
        else
        {
            LogRejected(logger, code);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Extensions = { [CodeKey] = code },
            },
        });
    }

    // Runs for every ProblemDetails: adds the correlation id, and gives framework-generated errors
    // (no code yet) a stable code and a Hungarian title instead of the English default.
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Extensions[CorrelationIdKey] = context.HttpContext.TraceIdentifier;
        if (!problem.Extensions.ContainsKey(CodeKey))
        {
            var status = problem.Status ?? StatusCodes.Status500InternalServerError;
            problem.Extensions[CodeKey] = DefaultCodeFor(status);
            problem.Title = DefaultTitleFor(status);
        }
    }

    public static int ToStatusCode(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => StatusCodes.Status400BadRequest,
        ErrorCategory.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorCategory.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCategory.NotFound => StatusCodes.Status404NotFound,
        ErrorCategory.Conflict => StatusCodes.Status409Conflict,
        ErrorCategory.RateLimited => StatusCodes.Status429TooManyRequests,
        ErrorCategory.BadGateway => StatusCodes.Status502BadGateway,
        ErrorCategory.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };

    public static string DefaultCodeFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "NOT_FOUND",
        StatusCodes.Status405MethodNotAllowed => "METHOD_NOT_ALLOWED",
        < 500 => "REQUEST_REJECTED",
        _ => "INTERNAL_ERROR",
    };

    private static string DefaultTitleFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "A kért oldal vagy adat nem található.",
        StatusCodes.Status405MethodNotAllowed => "Ez a művelet itt nem végezhető el.",
        < 500 => "A kérést nem sikerült feldolgozni. Ellenőrizd a megadott adatokat, és próbáld újra.",
        _ => "Váratlan hiba történt. Próbáld újra később.",
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed with {ErrorCode}")]
    private static partial void LogFailed(ILogger logger, Exception exception, string errorCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected with {ErrorCode}")]
    private static partial void LogRejected(ILogger logger, string errorCode);
}
