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

public sealed class NotFoundException(string code, string message)
    : AppException(ErrorCategory.NotFound, code, message);

public sealed class ConflictException(string code, string message)
    : AppException(ErrorCategory.Conflict, code, message);

public sealed class RateLimitedException(string code, string message)
    : AppException(ErrorCategory.RateLimited, code, message);

// A request-level 400 with its own code (e.g. ITEM_PROPOSAL_NO_ITEMS), when no single field is wrong.
public sealed class InvalidInputException(string code, string message)
    : AppException(ErrorCategory.Validation, code, message);

// ADR-0007: the external service (the LLM) answered something unusable.
public sealed class BadGatewayException(string code, string message)
    : AppException(ErrorCategory.BadGateway, code, message);

// ADR-0007: the external service (the LLM) is down, timed out or switched off.
public sealed class UnavailableException(string code, string message)
    : AppException(ErrorCategory.Unavailable, code, message);
