namespace KamraApp.Application.Common;

// ADR-0007: use cases signal expected failures with an AppException; the Api maps the category
// to an HTTP status in one place. Code is stable (clients and tests rely on it); Message is the
// safe Hungarian title shown to the user.
public abstract class AppException(ErrorCategory category, string code, string message) : Exception(message)
{
    public ErrorCategory Category { get; } = category;

    public string Code { get; } = code;
}

public enum ErrorCategory
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    RateLimited,
    BadGateway,
    Unavailable,
}
