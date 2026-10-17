namespace KamraApp.Application.Common;

// ADR-0008 6. / D1, D6: serialises the quantity, shopping list and suggestion changes of the same ingredients in
// a household until the use case's save commits (or the request ends). The caller locks before it reads the
// totals it decides on.
public interface IIngredientLocks
{
    Task LockAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken);
}
