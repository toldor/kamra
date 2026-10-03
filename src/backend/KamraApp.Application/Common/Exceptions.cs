namespace KamraApp.Application.Common;

// One general exception per ADR-0007 category in use; the stable code identifies the case.

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException(ErrorCategory.Validation, "VALIDATION_FAILED", "Néhány mező hibás. Javítsd a jelölt mezőket, és próbáld újra.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException ForField(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

public sealed class UnauthorizedException(string code, string message)
    : AppException(ErrorCategory.Unauthorized, code, message);

public sealed class ConflictException(string code, string message)
    : AppException(ErrorCategory.Conflict, code, message);

public sealed class RateLimitedException(string code, string message)
    : AppException(ErrorCategory.RateLimited, code, message);
