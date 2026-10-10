using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.Ingredients;

// The ingredient search (query string), validated like any request (ADR-0004).
public sealed class ListIngredientsQuery
{
    [StringLength(PantryInput.MaxSearchLength, ErrorMessage = PantryInput.SearchMessage)]
    public string? Search { get; init; }
}
