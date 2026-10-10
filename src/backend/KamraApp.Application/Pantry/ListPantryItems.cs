using KamraApp.Application.Common;

namespace KamraApp.Application.Pantry;

// US-1: the pantry list with search, category and "soon expiring" filters.
public sealed class ListPantryItems(IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public Task<IReadOnlyList<PantryItemResponse>> ExecuteAsync(string? search, string? category, bool? expiringSoon,
        CancellationToken cancellationToken) => throw new NotImplementedException($"red phase {pantry.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}
