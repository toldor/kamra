using KamraApp.Application.Common;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Cookings;

public static class CookingErrors
{
    public static NotFoundException RecipeNotFound() => new("RECIPE_NOT_FOUND", "Ez a recept nem található.");

    public static ConflictException IdempotencyConflict() =>
        new("IDEMPOTENCY_CONFLICT", "Ezt a főzést már más adatokkal rögzítetted. Töltsd újra az oldalt, és kezdd újra.");
}

// US-4: the stock is smaller than the confirmed used amount; the answer carries the fresh amounts so the
// screen can recalculate (ADR-0008 2. pont). Nothing is deducted.
public sealed class InsufficientStockException(IReadOnlyList<AvailableAmount> available)
    : AppException(ErrorCategory.Conflict, "INSUFFICIENT_STOCK",
        "Közben kevesebb lett a készlet, mint amennyit felhasználnál. Nézd át a friss mennyiségeket, és erősítsd meg újra.")
{
    public IReadOnlyList<AvailableAmount> Available { get; } = available;
}

// In the recipe's unit.
public sealed record AvailableAmount(Guid IngredientId, string IngredientName, decimal Amount, Unit Unit);
