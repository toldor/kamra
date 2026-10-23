using KamraApp.Application.Common;

namespace KamraApp.Application.ShoppingList;

// Stable codes and safe Hungarian titles of the shopping list use cases (error_handling.md).
public static class ShoppingListErrors
{
    // Missing and another household's row get the same answer (QA-1).
    public static NotFoundException ItemNotFound() =>
        new("SHOPPING_LIST_ITEM_NOT_FOUND", "Ez a tétel már nincs a bevásárlólistán.");

    public static NotFoundException SuggestionNotFound() =>
        new("SHOPPING_SUGGESTION_NOT_FOUND", "Ez a javaslat már nem érhető el. Frissítsd a listát.");

    // ADR-0008 4.: accepting a rejected suggestion or rejecting an accepted one.
    public static ConflictException SuggestionStateConflict() =>
        new("SHOPPING_SUGGESTION_STATE_CONFLICT", "Erről a javaslatról már másképp döntöttél. Frissítsd a listát.");
}
