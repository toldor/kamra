using KamraApp.Application.Common;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Cookings;

public interface ICookingRepository
{
    Task<Cooking?> FindByRequestAsync(Guid householdId, Guid requestId, CancellationToken cancellationToken);

    void Add(Cooking cooking, IEnumerable<StockMovement> movements);

    // Saves the cooking with the pantry changes in one transaction. Throws DuplicateCookingRequestException
    // when the request id is already taken, PANTRY_ITEM_MODIFIED when an item changed meanwhile.
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

// ADR-0008: the (household, request id) unique index rejected the save - the same request was saved
// meanwhile. ConfirmCooking answers it from the stored cooking; on its own it is an idempotency conflict.
public sealed class DuplicateCookingRequestException(Exception inner)
    : AppException(ErrorCategory.Conflict, CookingErrors.IdempotencyConflict().Code, CookingErrors.IdempotencyConflict().Message)
{
    public Exception Cause { get; } = inner;
}
