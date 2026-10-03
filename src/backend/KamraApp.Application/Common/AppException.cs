namespace KamraApp.Application.Common;

// ADR-0007: use cases signal expected failures with an AppException; the Api maps the category
// to an HTTP status in one place. Code is stable (clients and tests rely on it), Title is a safe
// Hungarian message shown to the user.
public abstract class AppException(ErrorCategory category, string code, string title) : Exception(title)
{
    public ErrorCategory Category { get; } = category;

    public string Code { get; } = code;

    public string Title { get; } = title;
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
