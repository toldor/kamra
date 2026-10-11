using KamraApp.Application.Common;

namespace KamraApp.Application.Pantry;

// Stable codes and safe Hungarian titles of the pantry use cases (error_handling.md).
public static class PantryErrors
{
    public static NotFoundException IngredientNotFound() =>
        new("INGREDIENT_NOT_FOUND", "Ez a hozzávaló nem található. Válassz a listából.");

    public static NotFoundException ItemNotFound() =>
        new("PANTRY_ITEM_NOT_FOUND", "Ez a tétel már nincs a kamrádban.");

    // ADR-0008: the item changed since the client read it; the client reloads it.
    public static ConflictException ItemModified() =>
        new("PANTRY_ITEM_MODIFIED", "Ezt a tételt közben máshol módosították. Nézd át a friss adatokat, és mentsd újra.");
}
