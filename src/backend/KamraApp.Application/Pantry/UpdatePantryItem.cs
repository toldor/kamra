using KamraApp.Application.Common;

namespace KamraApp.Application.Pantry;

// US-1: edit, decrease (with a reason) or delete (decrease to 0) a pantry item, guarded by its version.
public sealed class UpdatePantryItem(IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public Task<PantryItemResponse> ExecuteAsync(Guid id, UpdatePantryItemRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {pantry.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}
