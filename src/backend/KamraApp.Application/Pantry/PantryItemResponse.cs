using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Pantry;

// Amount is in the entered unit (30 dkg), Quantity in base units (300 g). Expired and SoonExpiring are
// computed for "today" in Budapest, so every client shows the same state.
public sealed record PantryItemResponse(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    decimal Amount,
    Unit Unit,
    decimal Quantity,
    Category Category,
    DateOnly ExpiryDate,
    bool ExpiryEstimated,
    bool Expired,
    bool SoonExpiring,
    uint Version)
{
    public static PantryItemResponse From(PantryItem item, string ingredientName, DateOnly today) =>
        throw new NotImplementedException();
}

// A pantry item with its ingredient's name, as the repository reads it.
public sealed record PantryEntry(PantryItem Item, string IngredientName);

public sealed record PantryFilter(string? NormalizedSearch, Category? Category, DateOnly? SoonExpiringOn);
