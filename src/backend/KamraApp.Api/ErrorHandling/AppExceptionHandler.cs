using KamraApp.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KamraApp.Api.ErrorHandling;

// ADR-0007: the single place where exceptions become ProblemDetails. Unknown exceptions never
// leak their message or stack trace; the details go to the log under the correlation id.
public sealed partial class AppExceptionHandler(IProblemDetailsService problemDetails, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public const string InternalErrorCode = "INTERNAL_ERROR";
    public const string InternalErrorTitle = "Váratlan hiba történt. Próbáld újra később.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            AppException app => (ToStatusCode(app.Category), app.Code, app.Title),
            _ => (StatusCodes.Status500InternalServerError, InternalErrorCode, InternalErrorTitle),
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
                Extensions = { ["code"] = code },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed with {ErrorCode}")]
    private static partial void LogFailed(ILogger logger, Exception exception, string errorCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected with {ErrorCode}")]
    private static partial void LogRejected(ILogger logger, string errorCode);

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
}
